resource "azurerm_virtual_network" "vnet_forum" {
    name                = "vnet-forum"
    address_space       = ["10.0.0.0/16"]
    location            = var.location
    resource_group_name = var.resource_group_name
}

resource "azurerm_subnet" "subnet_web_app" {
    name                 = "subnet-web-app"
    resource_group_name  = var.resource_group_name
    virtual_network_name = azurerm_virtual_network.vnet_forum.name
    address_prefixes     = ["10.0.1.0/24"]
}

resource "azurerm_subnet" "subnet_db" {
    name                 = "subnet-db"
    resource_group_name  = var.resource_group_name
    virtual_network_name = azurerm_virtual_network.vnet_forum.name
    address_prefixes     = ["10.0.2.0/24"]
}

resource "azurerm_private_dns_zone" "dns_zone" {
    name                = "privatelink.database.windows.net"
    resource_group_name = var.resource_group_name
}

resource "azurerm_private_dns_a_record" "sql_server_a_record" {
    name                = "sql-server-forum"
    zone_name           = azurerm_private_dns_zone.dns_zone.name
    resource_group_name = var.resource_group_name
    ttl                 = 300
    records             = [azurerm_mssql_server.sql_server_forum.fully_qualified_domain_name]
}

resource "azurerm_private_endpoint" "sql_server_private_endpoint" {
    name                = "sql-server-private-endpoint"
    location            = var.location
    resource_group_name = var.resource_group_name
    subnet_id           = azurerm_virtual_network.vnet_forum.id

    private_service_connection {
        name                           = "sql-server-connection"
        is_manual_connection           = false
        private_connection_resource_id = azurerm_mssql_server.sql_server_forum.id
        subresource_names              = ["sqlServer"]
    }
}

resource "azurerm_private_dns_zone_virtual_network_link" "dns_zone_vnet_link" {
    name                  = "dns-zone-vnet-link"
    resource_group_name   = var.resource_group_name
    private_dns_zone_name = azurerm_private_dns_zone.dns_zone.name
    virtual_network_id    = azurerm_virtual_network.vnet_forum.id
}