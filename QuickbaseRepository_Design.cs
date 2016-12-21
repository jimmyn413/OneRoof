using Intuit.QuickBase.Client;
using Intuit.QuickBase.Core;
using ORE.Service.Project.Models;
using ORE.Service.Project.Models.Projects;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ORE.Service.Project.Repositories
{
    public partial class QuickbaseRepository : IQuickbaseRepository
    {
        public bool InsertDesign(int projectId, DesignItem i)
        {
            var query = new Query();
            var fieldID = _projectColumnInfo[nameof(QBProjectItem.SunEdgeProjectId)].FieldId;
            query.Add(new QueryStrings(fieldID, ComparisonOperator.EX, projectId.ToString(), LogicalOperator.NONE));
            var modifiedqueue = new Dictionary<string, string>();
            var designplanequeue = new Dictionary<string, string>();
            var selectedFields = new List<int>();
            typeof(QBDesignItem).GetProperties().ToList().ForEach(p =>
                selectedFields.Add(_designColumnInfo[p.Name].FieldId));

            modifiedqueue.Add(nameof(QBDesignItem.StructuralStampRequired),
                i.StructuralStampRequired.ToString().ToLower() == "true" ? "Yes" : "No");

            modifiedqueue.Add(nameof(QBDesignItem.StructuralUpgradeRequired),
                i.StructuralUpgradeRequired.ToString().ToLower() == "true" ? "Yes" : "No");

            modifiedqueue.Add(nameof(QBDesignItem.RackingManufacturer),
                $"{i.RackingManufacturer}");

            modifiedqueue.Add(nameof(QBDesignItem.LoadJustificationRequired),
                i.LoadJustificationRequired.ToString().ToLower() == "true" ? "1" : "0");

            modifiedqueue.Add(nameof(QBDesignItem.IncentiveAmountPerkWh),
                i.IncentiveAmountPerkWh.ToString());

            modifiedqueue.Add(nameof(QBDesignItem.MspUpgradeBussRating),
                $"{i.MspUpgradeBussRating}");

            modifiedqueue.Add(nameof(QBDesignItem.MspUpgradeMainBreakerRating),
                $"{i.MspUpgradeMainBreakerRating}");

            modifiedqueue.Add(nameof(QBDesignItem.MspUpgradeMainBreakerLocation),
                $"{i.MspUpgradeMainBreakerLocation}");

            modifiedqueue.Add(nameof(QBDesignItem.HomeOwnerFinalDesignApproval),
                i.HomeOwnerFinalDesignApproval.ToString().ToLower() == "true" ? "Yes" : "No");

            modifiedqueue.Add(nameof(QBDesignItem.DesignNotes),
                $"{i.DesignNotes}");


            var qbDesignPlanes = new List<string> { "Second Plane Required",
                                                    "Third Plane Required",
                                                    "Fourth Plane Required",
                                                    "Fifth Plane Required",
                                                    "Sixth Plane Required" };

            qbDesignPlanes.ForEach(c => designplanequeue.Add(c, "0"));

            for (var j = 0; j < 6; j++)
            {
                string prefix = string.Format("MP{0} - ", j + 1);

                DesignPlaneItem p;

                if (i.DesignPlanes != null && i.DesignPlanes.Count > j)
                    { p = i.DesignPlanes[j]; }
                else
                    { p = new DesignPlaneItem(); }

                designplanequeue.Add((prefix + "# Modules (Max Fit)"), $"{(p.ModulesMaxFit == 0 ? null : p.ModulesMaxFit)}");
                designplanequeue.Add(prefix + "# Modules (Recommended)", $"{(p.ModulesRecommended == 0 ? null : p.ModulesRecommended)}");
                designplanequeue.Add(prefix + "Azimuth", $"{(p.Azimuth == 0 ? null : p.Azimuth)}");
                designplanequeue.Add(prefix + "MPPT 1 - String 1", $"{(p.Mppt_1_String_1 == 0 ? null : p.Mppt_1_String_1)}");

                if ((p.Mppt_1_String_1 != null && p.Mppt_1_String_1 != 0) && j > 0)
                    designplanequeue[qbDesignPlanes[j - 1]] = "1";//plane required checkbox dependent solely on mppt 1 string 1 being present or not

                designplanequeue.Add(prefix + "MPPT 1 - String 2", $"{(p.Mppt_1_String_2 == 0 ? null : p.Mppt_1_String_2)}");

                if (((p.Mppt_1_String_1 ?? 0) == 0)
                    && ((p.Mppt_1_String_2 ?? 0) == 0))

                    { designplanequeue.Add(prefix + "MPPT 1", "0"); }
                else
                    { designplanequeue.Add(prefix + "MPPT 1", "1"); }

                designplanequeue.Add(prefix + "MPPT 2 - String 1", $"{(p.Mppt_2_String_1 == 0 ? null : p.Mppt_2_String_1)}");
                designplanequeue.Add(prefix + "MPPT 2 - String 2", $"{(p.Mppt_2_String_2 == 0 ? null : p.Mppt_2_String_2)}");

                if (((p.Mppt_2_String_1 ?? 0) == 0) 
                    && ((p.Mppt_2_String_2 ?? 0) == 0))

                    { designplanequeue.Add(prefix + "MPPT 2", "0"); }
                else
                    { designplanequeue.Add(prefix + "MPPT 2", "1"); }

                designplanequeue.Add(prefix + "Pitch", $"{(p.Pitch == 0 ? null : p.Pitch)}");
                designplanequeue.Add(prefix + "SAP", $"{(p.SAP == 0 ? null : p.SAP)}");
                designplanequeue.Add(prefix + "TSRF", $"{(p.TSRF == 0 ? null : p.TSRF)}");
            }



            var projectTable = _client.Connect(_appID, new List<string> { _projectsID }).GetTable(_projectsID);

            projectTable.Query(query, selectedFields.ToArray());
            if (projectTable.Records.Count != 1)
                return false;

            var o = projectTable.Records.First();

            modifiedqueue.ToList().ForEach(q =>
            {
                o[_designColumnInfo[q.Key].FieldName] = q.Value;
            });

            designplanequeue.ToList().ForEach(q =>
            {
                o[q.Key] = q.Value;
            });

            o.AcceptChanges();




            var projectProductsTable = _client.Connect(_appID, new List<string> { _projectProductsID }).GetTable(_projectProductsID);
            var prodquery = new Query();

            prodquery.Add(new QueryStrings(146, ComparisonOperator.EX, projectId.ToString(), LogicalOperator.NONE));//146 is fieldid for sunedge project id in projectproductstable, NOT existing fieldID because different table (projecttable)

            projectProductsTable.Query(prodquery);

            projectProductsTable.Records.ForEach(r =>
                 new DeleteRecord(_client.Ticket, _appToken, _appDomain, _projectProductsID, r.RecordId).Post());
            if (i.DesignProducts != null)
            {
                foreach (var product in i.DesignProducts)
                {
                    var newProduct = projectProductsTable.NewRecord();
                    newProduct["Related Project"] = o["Project ID#"];
                    newProduct["Related Product"] = _designProductSunedgeQuickBaseMap[product.Product.Id].ToString();
                    newProduct["Quantity"] = product.Quantity.ToString();
                    newProduct.AcceptChanges();
                }
            }
            return true;
        }



        public Dictionary<int, int> GetProductSEtoQBMapping(List<ProductItem> productDictionary)
        {
            if (_designProductSunedgeQuickBaseMap != null)
                return _designProductSunedgeQuickBaseMap;

            _designProductSunedgeQuickBaseMap = new Dictionary<int, int>();

            productDictionary.ForEach(p => _designProductSunedgeQuickBaseMap.Add(p.Id, p.QBProductId));

            return _designProductSunedgeQuickBaseMap;
        }

    }



    public partial interface IQuickbaseRepository
    {
        bool InsertDesign(int projectId, DesignItem item);
        Dictionary<int, int> GetProductSEtoQBMapping(List<ProductItem> productDictionary);
    }
}
