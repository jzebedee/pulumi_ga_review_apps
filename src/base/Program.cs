using Pulumi;
using Pulumi.AzureNative;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.ManagedIdentity;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.WebPubSub.Inputs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

return await Pulumi.Deployment.RunAsync(() =>
{
    var az = new Pulumi.Config("azure-native");

    var subscriptionId = az.Require("subscriptionId");
    var location = az.Require("location");

    var config = new Pulumi.Config();

    var base_org = config.Require("base_org");
    var base_repo = config.Require("base_repo");

    Dictionary<string, object?> outputs = [];

    // base
    if (Pulumi.Deployment.Instance is { StackName: "base" })
    {
        var base_mi = BuildBaseManagedIdentity(subscriptionId, location, oidcOrg: base_org, oidcRepo: base_repo);
        outputs.Add("mi-shared-review:principalId", base_mi.PrincipalId);
    }
    else if (config.Require("pr_number") is string pr)
    {
        var uniqueId = $"{base_org}_{base_repo}_pr{pr}";
        var review_mi = BuildReviewManagedIdentity(subscriptionId, location, oidcOrg: base_org, oidcRepo: base_repo, uniqueId);
        outputs.Add("mi-review-app", review_mi.Name);
    }

    return outputs;
});

static UserAssignedIdentity BuildBaseManagedIdentity(string subscriptionId, string location, string oidcOrg, string oidcRepo)
{
    string rgName = "rg-shared-review";
    string identityName = "mi-shared-review";

    var managedIdentity = new UserAssignedIdentity(identityName, new()
    {
        Location = location,
        ResourceGroupName = rgName,
        ResourceName = identityName,
    });

    string fedCred_name = $"gh-pr_{oidcOrg}_{oidcRepo}";
    var federatedIdentityCredential = new FederatedIdentityCredential(fedCred_name, new()
    {
        Audiences = ["api://AzureADTokenExchange"],
        FederatedIdentityCredentialResourceName = fedCred_name,
        Issuer = "https://token.actions.githubusercontent.com",
        ResourceGroupName = rgName,
        ResourceName = identityName,
        Subject = $"repo:{oidcOrg}/{oidcRepo}:pull_request",
    });

    const string roleDefinitionId_managedIdentityContributor = "e40ec5ca-96e0-45a2-b4ff-59039f2c2b59";

    string ra_name = $"ra-managedIdentityContributor_{identityName}";
    string ra_roleDefId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId_managedIdentityContributor}";

    var roleAssignment = new RoleAssignment(ra_name, new()
    {
        PrincipalId = managedIdentity.PrincipalId,
        PrincipalType = PrincipalType.ServicePrincipal,
        Scope = $"/subscriptions/{subscriptionId}",
        RoleDefinitionId = ra_roleDefId,
    });

    return managedIdentity;
}

static UserAssignedIdentity BuildReviewManagedIdentity(string subscriptionId, string location, string oidcOrg, string oidcRepo, string uniqueId)
{
    string rgName = $"rg-review-{uniqueId}";
    string identityName = $"mi-review-{uniqueId}";

    var managedIdentity = new UserAssignedIdentity(identityName, new()
    {
        Location = location,
        ResourceGroupName = rgName,
        ResourceName = identityName,
    });

    string fedCred_name = $"gh-pr_{oidcOrg}_{oidcRepo}";
    var federatedIdentityCredential = new FederatedIdentityCredential(fedCred_name, new()
    {
        Audiences = ["api://AzureADTokenExchange"],
        FederatedIdentityCredentialResourceName = fedCred_name,
        Issuer = "https://token.actions.githubusercontent.com",
        ResourceGroupName = rgName,
        ResourceName = identityName,
        Subject = $"repo:{oidcOrg}/{oidcRepo}:pull_request",
    });

    const string roleDefinitionId_owner = "8e3af657-a8ff-443c-a75c-2fe8c4bcb635";

    string ra_name = $"ra-owner_{identityName}";
    string ra_roleDefId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId_owner}";

    var roleAssignment = new RoleAssignment(ra_name, new()
    {
        PrincipalId = managedIdentity.PrincipalId,
        PrincipalType = PrincipalType.ServicePrincipal,
        Scope = $"/subscriptions/{subscriptionId}/resourceGroups/{rgName}",
        RoleDefinitionId = ra_roleDefId,
    });

    return managedIdentity;
}