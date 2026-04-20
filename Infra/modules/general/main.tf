resource "azurerm_service_plan" "app_service_plan" {
  name                = var.app_service_plan_name
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Windows"
  sku_name            = "P0v4"
}

resource "azurerm_windows_web_app" "web_app_forum" {
  name                = "web-app-forum-api"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.app_service_plan.id
  https_only          = true

  app_settings = {
    ASPNETCORE_ENVIRONMENT     = "Production"
    ASPNETCORE_DETAILED_ERRORS = "true"
  }

  site_config {
    always_on = true

    application_stack {
      dotnet_version = "v10.0"
    }
  }

  connection_string {
    name  = "AZURE_SQL_CONNECTIONSTRING"
    type  = "SQLAzure"
    value = var.sql_connection_string
  }
}