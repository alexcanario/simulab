// F-66: Azure Communication Services Email for a cloud environment. Aspire has no hosting package for it, so the
// resources come from this template (AzureDeployment.AddEmail). No key, connection string or secret is created:
// the Api signs in with its managed identity.

@description('Principal id of the Api managed identity. Not named principalId: Aspire fills that name with the deploying account.')
param apiPrincipalId string

@description('Where the services keep their data at rest (F-66 BR8, ADR-0003). Both resources must use the same value.')
param dataLocation string = 'Brazil'

// Communication Service host names are global, so the names carry the resource group.
var suffix = uniqueString(resourceGroup().id)

// Built-in "Communication and Email Service Owner". It grants more than sending, but a custom send-only role needs
// roleDefinitions/write, which the deploying account does not have; the scope below is what keeps it narrow.
var communicationAndEmailServiceOwner = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '09976791-48a7-449e-bb21-39d1a415f350'
)

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: 'simulab-email-${suffix}'
  location: 'global'
  properties: {
    dataLocation: dataLocation
  }
}

// The Azure-managed domain sends from <id>.azurecomm.net; Simulab's own domain is F-87.
resource domain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  properties: {
    domainManagement: 'AzureManaged'
  }
}

resource sender 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-04-01' = {
  parent: domain
  name: 'donotreply'
  properties: {
    username: 'DoNotReply'
    displayName: 'Simulab'
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: 'simulab-comm-${suffix}'
  location: 'global'
  properties: {
    dataLocation: dataLocation
    linkedDomains: [
      domain.id
    ]
  }
}

// Only the Api's identity, only on this Communication Service. The Web gets nothing.
resource apiMaySend 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(communicationService.id, apiPrincipalId, communicationAndEmailServiceOwner)
  scope: communicationService
  properties: {
    roleDefinitionId: communicationAndEmailServiceOwner
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output endpoint string = 'https://${communicationService.properties.hostName}'
output senderAddress string = 'DoNotReply@${domain.properties.fromSenderDomain}'
