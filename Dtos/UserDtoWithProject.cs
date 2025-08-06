    using TimeRecorderBACKEND.Dtos;

    public class UserDtoWithProject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public ProjectDto? Project { get; set; }
    }