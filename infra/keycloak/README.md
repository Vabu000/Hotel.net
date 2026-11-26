# Keycloak infrastructure

This Pulumi stack provisions the `HotelRealm` realm, authorization resource servers and scopes for reservations, catalog, payments and reviews, role-based scope permissions, OAuth clients, and one test user per role.

From this directory, configure the provider URL and credentials without committing them:

```powershell
pulumi config set keycloakUrl http://localhost:8080
pulumi config set adminUser admin
pulumi config set --secret adminPassword "<Keycloak admin password>"
pulumi config set --secret aggregatorClientSecret "<service client secret>"
pulumi config set --secret testUsers:adminPassword "<test user password>"
pulumi config set --secret testUsers:managerPassword "<test user password>"
pulumi config set --secret testUsers:userPassword "<test user password>"
pulumi config set --secret testUsers:guestPassword "<test user password>"
pulumi up
```

The password values are required stack configuration. They are not stored in this repository.
