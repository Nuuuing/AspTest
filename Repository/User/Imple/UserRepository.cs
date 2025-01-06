using MySqlConnector;
using Dapper;
using System.Data;

public class UserRepository : IUserRepository {

    private readonly string _connectionString;

    public UserRepository(string connectionString) {
        _connectionString = connectionString;
    }

    public int CreateUser(UserCreateDto userCreateDto) {
        using (IDbConnection db = new MySqlConnection(_connectionString)) {
            string sql = @"INSERT INTO tb_user_info
                (user_id, user_name, user_pwd, user_email, account_locked, last_pwd_date, create_date, update_date, provider)
                VALUES (@userId, @userName, @userPwd, @userEmail, false, NOW(), NOW(), NOW(), @provider);
                SELECT LAST_INSERT_ID();";
            return db.ExecuteScalar<int>(sql, userCreateDto); // 삽입된 No를 정수로 반환
        }
    }

    public int CreateDefaultUserData(int userNo) {
        using (IDbConnection db = new MySqlConnection(_connectionString)) {
            string sql = @"INSERT INTO tb_user_data" +
                "(user_no, coin_amount, token_amount, update_date)" +
                "VALUES (@userNo, 0, 5, NOW())";
            int rowsAffected = db.Execute(sql, new { userNo });
            return rowsAffected;
        }
    }
}
