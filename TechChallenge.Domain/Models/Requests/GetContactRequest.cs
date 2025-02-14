namespace TechChallenge.Domain.Models.Requests
{
    public class GetContactRequest
    {
        public int? DDD { get; set; }

        public GetContactRequest(int? ddd)
        {
            DDD = ddd;
        }
    }
}
