resource "azurerm_mssql_server" "sql_server_forum" {
  name                         = "forum-app-sql-server"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"
  public_network_access_enabled = true
}

resource "azurerm_mssql_database" "db_forum" {
  name      = "web-app-forum-api-database"
  server_id = azurerm_mssql_server.sql_server_forum.id
  sku_name  = "GP_Gen5_2"

  depends_on = [azurerm_mssql_server.sql_server_forum]
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql_server_forum.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}