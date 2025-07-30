namespace TimeRecorderBACKEND.Dtos
{
    public class UserInfoDto
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public bool IsAuthenticated { get; set; } = false;
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}