// F-33 moved the Api host, the accounts and the token client to Simulab.Testing, where every module's
// tests can reach them. These aliases keep the names this project's 40-odd test files already use.
global using Accounts = Simulab.Testing.ApiHost.TestAccounts;
global using IdentityApiFactory = Simulab.Testing.ApiHost.SimulabApiFactory;
global using TestClient = Simulab.Testing.ApiHost.TestClient;
global using TokenClient = Simulab.Testing.ApiHost.TokenClient;
global using TokenResponse = Simulab.Testing.ApiHost.TokenResponse;
