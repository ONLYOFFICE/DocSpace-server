extern alias ASCWebApi;
extern alias ASCFiles;
extern alias ASCPeople;
global using System.Data.Common;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Net.Http.Json;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Web;

global using ASC.Api.Core;
global using ASC.Api.Core.Middleware;
global using ASC.Core.Users;
global using ASC.Files.Core;
global using ASC.Files.Core.ApiModels.RequestDto;
global using ASC.Files.Core.ApiModels.ResponseDto;
global using ASC.Files.Core.Security;
global using ASC.Files.Tests1.Data;
global using ASC.Files.Tests1.Models;
global using ASC.Migrations;
global using ASC.Migrations.Core;
global using ASC.Security.Cryptography;
global using ASC.Web.Core.Utility.Settings;

global using ASCPeople::ASC.People.ApiModels.RequestDto;

global using ASCWebApi::ASC.Web.Api.ApiModel.RequestsDto;
global using ASCWebApi::ASC.Web.Api.ApiModel.ResponseDto;

global using FluentAssertions;

global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using MySql.Data.MySqlClient;

global using Respawn;
global using Respawn.Graph;

global using Testcontainers.MySql;
global using Xunit;

global using FilesProgram = ASCFiles::Program;
global using WebApiProgram = ASCWebApi::Program;
global using PeopleProgram = ASCPeople::Program;