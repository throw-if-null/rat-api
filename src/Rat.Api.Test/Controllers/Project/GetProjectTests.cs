﻿using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rat.Api.Routes.Data;
using Rat.Sql;
using Snapshooter.Xunit;
using Xunit;

namespace Rat.Api.Test.Controllers.Project
{
	[Collection("Integration")]
    public class GetProjectTests
    {
        private readonly RatFixture _fixture;

        public GetProjectTests(RatFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Should_Get_Project_By_Id()
        {
            using var scope = _fixture.Provider.CreateScope();
			var connectionFactory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
			await using var connection = connectionFactory.CreateConnection();

			var command = new CommandDefinition("SELECT Id FROM ProjectType WHERE Name = @Name", new { Name = "csharp" });
			var projectTypeId = await connection.QuerySingleAsync<int>(command);

			command = new CommandDefinition(
				"INSERT INTO Project (Name, ProjectTypeId, Operator, Operation) VALUES(@Name, @ProjectTypeId, 1, N'insert'); SELECT SCOPE_IDENTITY()",
				new { Name = "Should_Get_Project_By_Id", ProjectTypeId = projectTypeId });

			var project = await connection.ProjectInsert("Should_Get_Project_By_Id", projectTypeId, 1, CancellationToken.None);
			var projectId = project.Id;

            var response = await _fixture.Client.GetAsync($"/api/projects/{projectId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var contentStream = await response.Content.ReadAsStreamAsync();
            var content = await JsonSerializer.DeserializeAsync<GetProjectRouteOutput>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Snapshot.Match(
                content,
                x =>
                {
                    x.IgnoreField<int>("Id");
                    x.IgnoreField<int>("TypeId");

                    return x;
                });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-4)]
        public async Task Should_Return_BadRequest(int id)
        {
            var projectId = id.ToString();

            var response = await _fixture.Client.GetAsync($"/api/projects/{projectId}");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Should_Return_Projects_For_User()
        {
			var authProviderUserId = Guid.NewGuid().ToString().Substring(0, 12);

			using var scope = _fixture.Provider.CreateScope();
			var connectionFactory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
			await using var connection = connectionFactory.CreateConnection();

			var command = new CommandDefinition("SELECT Id FROM ProjectType WHERE Name = @Name", new { Name = "csharp" });
			var projectTypeId = await connection.QuerySingleAsync<int>(command);

			var memberId = await connection.MemberInsert(authProviderUserId, 1, CancellationToken.None);
			var projectA = await connection.ProjectInsert("Project A", projectTypeId, 1, CancellationToken.None);
			int projectIdA = projectA.Id;
			var projectB = await connection.ProjectInsert("Project B", projectTypeId, 1, CancellationToken.None);
			int projectIdB = projectB.Id;
			await connection.MemberProjectInsert(memberId, projectIdA, 1, CancellationToken.None);
			await connection.MemberProjectInsert(memberId, projectIdB, 1, CancellationToken.None);

			var request = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri("/api/projects/", uriKind: UriKind.Relative)
			};

			request.Headers.Add("test-user", memberId.ToString());

			var response = await _fixture.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (var content = await response.Content.ReadAsStreamAsync())
            {
                var projects =
                    await JsonSerializer.DeserializeAsync<GetProjectForUserRouteOutput>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Snapshot.Match(
                    projects,
                    x =>
                    {
						x.IgnoreFields<int>("UserId");
						x.IgnoreFields<int>("ProjectStats[*].Id");

						return x;
                    });
            }
        }

        [Fact]
        public async Task Should_Return_No_Projects_When_User_Is_Not_In_Rat_Database()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("/api/projects", UriKind.Relative)
            };

            request.Headers.Add("test-user", "31");

            var response = await _fixture.Client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (var content = await response.Content.ReadAsStreamAsync())
            {
				var projects =
                    await JsonSerializer.DeserializeAsync<GetProjectForUserRouteOutput>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Assert.NotEqual(0, projects.UserId);
                Assert.Empty(projects.ProjectStats);
            }
        }

        [Fact]
        public async Task Should_Return_Forbidden_When_NameClaim_Is_Missing()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("/api/projects", UriKind.Relative)
            };

            request.Headers.Add("test-user", "null");

            var response = await _fixture.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
