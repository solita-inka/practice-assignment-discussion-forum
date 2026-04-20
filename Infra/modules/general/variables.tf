variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "sql_connection_string" {
  description = "SQL Server connection string"
  type        = string
  sensitive   = true
}

variable "app_service_plan_name" {
  description = "Name of the App Service Plan"
  type        = string
}