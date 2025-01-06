public class UserService : IUserService {
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository) {
        _userRepository = userRepository;
    }

    public int CreateUser(UserCreateDto userCreateDto) {
        int userNo = _userRepository.CreateUser(userCreateDto);
        Console.WriteLine($"CreateUser returned: {userNo}");

        if (userNo > 0) {
            return _userRepository.CreateDefaultUserData(userNo);
        }
        else return 0;
    }
}
