resource "azurerm_storage_account" "function_storage" {
  name                     = var.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_windows_function_app" "function_app" {
    name                       = var.function_app_name
    location                   = var.location
    resource_group_name        = var.resource_group_name
    service_plan_id            = var.service_plan_id
    storage_account_name       = azurerm_storage_account.function_storage.name
    storage_account_access_key = azurerm_storage_account.function_storage.primary_access_key

    app_settings = {
        "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
        "WEBSITE_RUN_FROM_PACKAGE" = "1"
    }

    site_config {}
}