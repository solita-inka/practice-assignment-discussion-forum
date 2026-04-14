variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "sql_admin_login" {
  description = "SQL Server admin username"
  type        = string
  default     = "forum-app-server-admin"
}

variable "sql_admin_password" {
  description = "SQL Server admin password"
  type        = string
  sensitive   = true
}