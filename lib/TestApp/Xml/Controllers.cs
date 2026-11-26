namespace DarkPatterns.OpenApiCodegen.Server.Mvc.TestApp.Xml;

public class BooksController : BooksControllerBase
{
	protected override async Task<AddBookActionResult> AddBook(Book addBookBody)
	{
		this.DelegateRequest<Book?>(addBookBody);
		await this.DelegateResponse<AddBookActionResult>();
		return AddBookActionResult.Ok(addBookBody);
	}
}
