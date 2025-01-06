public interface IUserRepository {

    int CreateUser(UserCreateDto userCreateDto);

    int CreateDefaultUserData(int userNo);
}
