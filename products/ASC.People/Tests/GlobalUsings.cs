extern alias ASCWebApi;
extern alias ASCPeople;
global using System.Data.Common;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Text;
global using System.Web;

global using ASC.Core.Common.EF;
global using ASC.Migrations;
global using ASC.Migrations.Core;

global using Bogus;
global using Bogus.DataSets;

global using DocSpace.API.SDK.Api;
global using DocSpace.API.SDK.Client;
global using DocSpace.API.SDK.Model;

global using DotNet.Testcontainers.Builders;
global using DotNet.Testcontainers.Containers;

global using FluentAssertions;

global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using MySql.Data.MySqlClient;

global using Npgsql;

global using Respawn;
global using Respawn.Graph;

global using Testcontainers.MySql;
global using Testcontainers.PostgreSql;
global using Testcontainers.RabbitMq;
global using Testcontainers.Redis;

global using Xunit;

global using WebApiProgram = ASCWebApi::Program;
global using PeopleProgram = ASCPeople::Program;