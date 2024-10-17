namespace DarkPatterns.TestApp.MinimalApis.Annotations
{
	public class DogController : DogControllerBase
	{
		protected override Task<AddDogActionResult> AddDog(Dog addDogBody)
		{
			this.DelegateRequest(addDogBody);
			return this.DelegateResponse<AddDogActionResult>();
		}
	}
}
