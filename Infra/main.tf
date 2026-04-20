resource "azurerm_resource_group" "rg_forum_app" {
  name     = "rg-inkan-forum-app"
  location = var.location
  tags = {
    Owner   = var.owner_email
    DueDate = var.due_date
  }
}

module "general" {
  source               = "./modules/general"
  resource_group_name  = azurerm_resource_group.rg_forum_app.name
  location             = azurerm_resource_group.rg_forum_app.location
  sql_connection_string  = "Server=${module.database.sql_server_fqdn};Database=${module.database.database_name};User Id=${module.database.sql_admin_login};Password=${var.sql_admin_password};TrustServerCertificate=True;"
  app_service_plan_name = var.app_service_plan_name
}

module "database" {
  source              = "./modules/database"
  resource_group_name = azurerm_resource_group.rg_forum_app.name
  location            = azurerm_resource_group.rg_forum_app.location
  sql_admin_password  = var.sql_admin_password
}

module "functions" {
  source               = "./modules/functions"
  resource_group_name  = azurerm_resource_group.rg_forum_app.name
  location             = azurerm_resource_group.rg_forum_app.location
  function_app_name    = "forum-function-app-archive-timer"
  service_plan_id      = module.general.app_service_plan_id
  storage_account_name = var.storage_account_name
}
