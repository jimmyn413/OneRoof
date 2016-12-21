using ORE.Service.Project.CommonCandidates;
using ORE.Service.Project.Enums;
using ORE.Service.Project.Models;
using ORE.Service.Project.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;

namespace ORE.Service.Project.Repositories
{
    public partial class SunedgeRepository : ISunedgeRepository
    {
        // static, load these once per service lifetime and treat as enums from db
        private static List<ProjectConfigurationLookup> _projectConfigurationLookup;

        public List<ProjectConfigurationItem> GetProjectConfigurationItems(int projectId, ConfigurationItemType configItemType)
        {
            var list = new List<ProjectConfigurationItem>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("vwJobItemGroupsGet", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("JobId", projectId);
                cmd.Parameters.AddWithValue("@ConfigurationItemTypeId", configItemType);

                cmd.Connection.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var i = new ProjectConfigurationItem();
                        i.Key = rdr["ConfigurationItemName"].ToString();
                        i.Value = string.IsNullOrEmpty(rdr["Value"].ToString()) ? null : rdr["Value"] as string;
                        i.ValueType = Enum.GetName(typeof(ConfigurationItemValueType), Convert.ToInt32(rdr["ConfigurationItemValueTypeId"]));
                        list.Add(i);
                    }
                }
            }

            if (list.Count > 0)
                return list;

            if (_projectConfigurationLookup == null)
                GetProjectConfigurationLookup();

            //if null, populatinig list of key: site characteristic, value: null, valuetype: null for appian
            _projectConfigurationLookup.Where(x => x.ConfigurationItemType == configItemType).ToList()
                .ForEach(c => list.Add(
                    new ProjectConfigurationItem { Key = c.ItemName, Value = null, ValueType = c.ItemValueTypeName }));


            return list;
        }

        public ServiceResult<ProjectConfigurationItem> InsertProjectConfigurationItems(
            int projectId, List<ProjectConfigurationItem> item, UserContext2 user, ConfigurationItemType configItemType)
        {
            int jobItemGroupId;
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("JobItemGroupInsert", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@JobId", projectId);
                cmd.Parameters.AddWithValue("@ConfigurationItemTypeId", configItemType);
                cmd.Parameters.AddWithValue("@UserName", user.UserName);
                cmd.Connection.Open();
                jobItemGroupId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("JobItemGroupItemInsert", conn))
            {
                cmd.Connection.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                foreach (var ci in item)
                {
                    
                    var projectConfiguration = GetProjectConfigurationLookup().FirstOrDefault(s =>
                    s.ItemName.ToLower() == ci.Key.ToLower() && s.ConfigurationItemType == configItemType);

                    if (projectConfiguration == null)
                        continue;

                    cmd.Parameters.Clear();
                    
                    cmd.Parameters.AddWithValue("@JobItemGroupId", jobItemGroupId);
                    cmd.Parameters.AddWithValue("@ConfigurationItemId", projectConfiguration.ItemId);
                    cmd.Parameters.AddWithValue("@Value", (object)ci.Value ?? string.Empty);
                    cmd.Parameters.AddWithValue("@UserName", user.UserName);

                    cmd.ExecuteNonQuery();
                }
            }

            return new ServiceResult<ProjectConfigurationItem>(HttpStatusCode.OK);
        }

        private List<ProjectConfigurationLookup> GetProjectConfigurationLookup()
        {
            if (_projectConfigurationLookup != null)
                return _projectConfigurationLookup;

            _projectConfigurationLookup = new List<ProjectConfigurationLookup>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("ConfigurationItemGet", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        _projectConfigurationLookup.Add(new ProjectConfigurationLookup
                        {
                            ItemId = Convert.ToInt32(rdr["ConfigurationItemId"]),
                            ItemName = rdr["ConfigurationItemName"].ToString(),
                            ItemValueTypeName = Enum.GetName(typeof(ConfigurationItemValueType), Convert.ToInt32(rdr["ConfigurationItemValueTypeId"])),
                            ConfigurationItemType = (ConfigurationItemType)Convert.ToInt32(rdr["ConfigurationItemTypeId"])
                        });
                    }
                }
            }

            return _projectConfigurationLookup;
        }
    }

    public partial interface ISunedgeRepository
    {
        List<ProjectConfigurationItem> GetProjectConfigurationItems(int projectId, ConfigurationItemType configItemType);
        ServiceResult<ProjectConfigurationItem> InsertProjectConfigurationItems(int projectId, List<ProjectConfigurationItem> item, UserContext2 user, ConfigurationItemType configItemType);
    }
}
