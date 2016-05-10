using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace WorkerHost.Data.Outputs
{
    public class SQLOutputRepository
    {
        private const string ValueTableField = "value";
        private const string GuidTableField = "guid";
        private const string OrganizationTableField = "organization";
        private const string DisplayNameTableField = "displayname";
        private const string UnitOfMeasureTableField = "unitofmeasure";
        private const string MeasureNameTableField = "measurename";
        private const string LocationTableField = "location";
        private const string TimeCreatedTableField = "timecreated";

        private const string InsertStoreProcedure = "InsertAlertsData";
        private const string InsertParameter = "@dataList";

        private const string TableType = "AlertsDataTableType";

        private readonly string _sqlDatabaseConnectionString;

        public SQLOutputRepository(string sqlDatabaseConnectionString)
        {
            _sqlDatabaseConnectionString = sqlDatabaseConnectionString;
        }

        public void ProcessEvents(IList<SensorDataContract> eventDataList)
        {
            if (eventDataList == null)
            {
                return;
            }

            try
            {
                using (var sqlConnection = new SqlConnection(_sqlDatabaseConnectionString))
                {
                    sqlConnection.Open();

                    var table = new DataTable();

                    // Add columns to the table
                    table.Columns.Add(ValueTableField, typeof(double));
                    table.Columns.Add(GuidTableField, typeof(string));
                    table.Columns.Add(OrganizationTableField, typeof(string));
                    table.Columns.Add(DisplayNameTableField, typeof(string));
                    table.Columns.Add(UnitOfMeasureTableField, typeof(string));
                    table.Columns.Add(MeasureNameTableField, typeof(string));
                    table.Columns.Add(LocationTableField, typeof(string));
                    table.Columns.Add(TimeCreatedTableField, typeof(DateTime));

                    // Add rows to the table
                    foreach (var eventData in eventDataList)
                    {
                        table.Rows.Add(eventData.Value,
                            eventData.Guid,
                            eventData.Organization,
                            eventData.DisplayName,
                            eventData.UnitOfMeasure,
                            eventData.MeasureName,
                            eventData.Location,
                            eventData.TimeCreated);
                    }

                    // Create command
                    var sqlCommand = new SqlCommand(InsertStoreProcedure, sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    // Add table-valued parameter
                    sqlCommand.Parameters.Add(new SqlParameter
                        {
                            ParameterName = InsertParameter,
                            SqlDbType = SqlDbType.Structured,
                            TypeName = TableType,
                            Value = table,
                        });

                    // Execute the query
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
            }
        }

    }
}
