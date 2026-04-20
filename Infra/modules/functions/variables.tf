variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}
variable "function_app_name" {
  type = string
}

variable "service_plan_id" {
  type = string
}

variable "storage_account_name" {
  description = "Name of the storage account for the function app"
  type        = string
}