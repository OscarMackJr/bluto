output "deployment_contract" {
  description = "Reviewable nonproduction deployment contract. It intentionally contains no credential values."
  value       = local.deployment_contract
}