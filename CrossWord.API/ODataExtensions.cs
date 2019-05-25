using System;
using Microsoft.AspNet.OData.Query;
using Microsoft.OData.UriParser;

namespace CrossWord.API
{
    public static class ODataExtensions
    {
        // https://www.ben-morris.com/parsing-odata-queries-decoupled-data-entities-webapi/

        public static string OrderByClause(this ODataQueryOptions options)
        {
            // Order by, e.g. /Products?$orderby=Supplier asc,Price desc
            string s = "";
            if (options.OrderBy != null && options.OrderBy.OrderByClause != null)
            {
                var orderByNodes = options.OrderBy.OrderByNodes;
                int totalCount = orderByNodes.Count;

                s = " ORDER BY ";

                for (int count = 0; count < totalCount; count++)
                {
                    var typedNode = orderByNodes[count] as OrderByPropertyNode;
                    var name = typedNode.Property.Name;
                    var direction = typedNode.OrderByClause.Direction;

                    // add field value
                    s += $"{name} ";

                    // add direction
                    switch (direction)
                    {
                        case OrderByDirection.Ascending:
                            s += "asc";
                            break;
                        case OrderByDirection.Descending:
                            s += "desc";
                            break;
                    }

                    count++;

                    // do something with each item
                    if (count == totalCount)
                    {
                        // do something different with the last item
                    }
                    else
                    {
                        // do something different with every item but the last
                        s += ", ";
                    }
                }
            }
            return s;
        }

        public static string TopAndSkipClause(this ODataQueryOptions options)
        {
            // Top and skip, e.g. /Products?$top=10&$skip=20
            string s = "";

            if (options.Top != null)
            {
                s += $" LIMIT {options.Top.Value}";
            }

            if (options.Skip != null)
            {
                s += $" OFFSET {options.Skip.Value}";
            }

            return s;
        }

        public static string WhereClause(this ODataQueryOptions options)
        {
            // Parsing a filter, e.g. /Users?$filter=Id eq '1' or Id eq '100'  
            string s = "";
            if (options.Filter != null && options.Filter.FilterClause != null)
            {
                throw new NotImplementedException();
            }
            return s;
        }
    }
}