using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp;

using static Utilities;
using Xml;

public class XmlYamlShould
{
	private const string BookXml = """
		<book id="7">
			<book-title>Dune</book-title>
			<authors>
				<author>Frank Herbert</author>
			</authors>
		</book>
		""";

	[Fact(Skip = "OpenAPI 3.0 gap (XML): application/xml returns 415; there is no XML formatter and the XML Object is ignored")]
	public Task Round_trip_xml_bodies_shaped_by_the_xml_object() =>
		TestSingleRequest<BooksControllerBase.AddBookActionResult, Book?>(new(
			BooksControllerBase.AddBookActionResult.Unsafe(new NoContentResult()),
			client => client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/xml/books")
			{
				Content = new StringContent(BookXml, Encoding.UTF8, "application/xml"),
				Headers = { { "Accept", "application/xml" } },
			})
		)
		{
			AssertRequest = (controller, body) =>
			{
				Assert.True(controller.ModelState.IsValid);
				Assert.Equal(7, body?.Id);
				Assert.Equal("Dune", body?.Title);
				Assert.Equal(["Frank Herbert"], body?.Authors?.Value);
			},
			AssertResponseMessage = async (response) =>
			{
				Assert.Equal(200, (int)response.StatusCode);
				Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
				var book = XElement.Parse(await response.Content.ReadAsStringAsync());
				Assert.Equal("book", book.Name.LocalName);
				Assert.Equal("7", book.Attribute("id")?.Value);
				Assert.Equal("Dune", book.Element("book-title")?.Value);
				Assert.Equal(["Frank Herbert"], book.Element("authors")?.Elements("author").Select(e => e.Value));
			},
		});
}
