output "sql_server_fqdn" {
  value = azurerm_mssql_server.sql_server_forum.fully_qualified_domain_name
}

output "database_name" {
  value = azurerm_mssql_database.db_forum.name
}

output "sql_admin_login" {
  value = azurerm_mssql_server.sql_server_forum.administrator_login
}