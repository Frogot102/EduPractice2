using BCrypt.Net;
using EduPractice2.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace EduPractice2.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==========================================
        // 1. АВТОРИЗАЦИЯ И РЕГИСТРАЦИЯ
        // ==========================================

        public async Task<List<Role>> GetRolesAsync()
        {
            var roles = new List<Role>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT id_role, name FROM roles ORDER BY id_role", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                roles.Add(new Role
                {
                    IdRole = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            return roles;
        }

        public async Task<Role?> GetUserRoleAsync(int roleId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT id_role, name FROM roles WHERE id_role = @id", conn);
            cmd.Parameters.AddWithValue("id", roleId);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Role
                {
                    IdRole = reader.GetInt32(0),
                    Name = reader.GetString(1)
                };
            }
            return null;
        }

        public async Task<User?> AuthenticateAsync(string login, string password)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT id_user, login, password, role_id, full_name, photo FROM users WHERE login = @login", conn);
            cmd.Parameters.AddWithValue("login", login);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var storedHash = reader.GetString(2);
                // Проверка пароля через BCrypt
                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                {
                    return new User
                    {
                        IdUser = reader.GetInt32(0),
                        Login = reader.GetString(1),
                        Password = storedHash,
                        RoleId = reader.GetInt32(3),
                        FullName = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Photo = reader.IsDBNull(5) ? null : reader.GetString(5)
                    };
                }
            }
            return null;
        }

        public async Task<bool> RegisterUserAsync(string login, string password, int roleId, string? fullName)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Проверка уникальности логина
            await using var checkCmd = new NpgsqlCommand(
                "SELECT 1 FROM users WHERE login = @login", conn);
            checkCmd.Parameters.AddWithValue("login", login);

            if (await checkCmd.ExecuteScalarAsync() != null)
                return false;

            // Хеширование пароля
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            await using var cmd = new NpgsqlCommand(
                "INSERT INTO users (login, password, role_id, full_name) VALUES (@login, @password, @role_id, @full_name)", conn);
            cmd.Parameters.AddWithValue("login", login);
            cmd.Parameters.AddWithValue("password", hashedPassword);
            cmd.Parameters.AddWithValue("role_id", roleId);
            cmd.Parameters.AddWithValue("full_name", fullName ?? (object)DBNull.Value);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ==========================================
        // 2. УЧЕТ РАБОТНИКОВ (ДЛЯ ДИРЕКТОРА)
        // ==========================================

        /// <summary>
        /// Получает список всех работников (JOIN с таблицей Users для получения ФИО и Роли)
        /// </summary>
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var employees = new List<Employee>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Запрос объединяет employees, users и roles
            var sql = @"
                SELECT 
                    e.id_employee, 
                    e.id_user, 
                    u.full_name, 
                    e.birth_date, 
                    e.address, 
                    e.education, 
                    e.qualification, 
                    e.operations_list, 
                    u.role_id, 
                    r.name as role_name
                FROM employees e
                JOIN users u ON e.id_user = u.id_user
                JOIN roles r ON u.role_id = r.id_role
                ORDER BY u.full_name";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                employees.Add(new Employee
                {
                    IdEmployee = reader.GetInt32(0),
                    IdUser = reader.GetInt32(1),
                    FullName = reader.IsDBNull(2) ? "Не указано" : reader.GetString(2),
                    BirthDate = reader.GetDateTime(3),
                    Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Education = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Qualification = reader.IsDBNull(6) ? null : reader.GetString(6),
                    OperationsList = reader.IsDBNull(7) ? null : reader.GetString(7),
                    RoleId = reader.GetInt32(8),
                    RoleName = reader.IsDBNull(9) ? null : reader.GetString(9)
                });
            }
            return employees;
        }

        /// <summary>
        /// Получает список пользователей, которые НЕ являются "Заказчиками" 
        /// и еще не добавлены в таблицу работников. Используется для ComboBox при создании.
        /// </summary>
        public async Task<List<User>> GetWorkersUsersAsync()
        {
            var users = new List<User>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT 
                    u.id_user, 
                    u.login, 
                    u.full_name, 
                    u.role_id, 
                    r.name
                FROM users u
                JOIN roles r ON u.role_id = r.id_role
                LEFT JOIN employees e ON u.id_user = e.id_user
                WHERE e.id_user IS NULL 
                  AND r.name != 'Заказчик'  -- Исключаем заказчиков
                ORDER BY u.full_name";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    IdUser = reader.GetInt32(0),
                    Login = reader.GetString(1),
                    FullName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    RoleId = reader.GetInt32(3)
                });
            }
            return users;
        }

        public async Task<bool> AddEmployeeAsync(Employee emp)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Проверяем, существует ли такой пользователь
            await using var checkCmd = new NpgsqlCommand(
                "SELECT 1 FROM users WHERE id_user = @id", conn);
            checkCmd.Parameters.AddWithValue("id", emp.IdUser);

            var exists = await checkCmd.ExecuteScalarAsync() != null;
            if (!exists) return false;

            // Проверяем, не является ли он уже работником
            await using var checkEmpCmd = new NpgsqlCommand(
                "SELECT 1 FROM employees WHERE id_user = @id", conn);
            checkEmpCmd.Parameters.AddWithValue("id", emp.IdUser);

            if (await checkEmpCmd.ExecuteScalarAsync() != null) return false;

            // Добавляем запись
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO employees (id_user, birth_date, address, education, qualification, operations_list) 
                VALUES (@id_user, @bd, @addr, @edu, @qual, @ops)", conn);

            cmd.Parameters.AddWithValue("id_user", emp.IdUser);
            cmd.Parameters.AddWithValue("bd", emp.BirthDate);
            cmd.Parameters.AddWithValue("addr", emp.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("edu", emp.Education ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("qual", emp.Qualification ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ops", emp.OperationsList ?? (object)DBNull.Value);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateEmployeeAsync(Employee emp)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                UPDATE employees 
                SET birth_date = @bd, address = @addr, education = @edu, 
                    qualification = @qual, operations_list = @ops 
                WHERE id_employee = @id", conn);

            cmd.Parameters.AddWithValue("id", emp.IdEmployee);
            cmd.Parameters.AddWithValue("bd", emp.BirthDate);
            cmd.Parameters.AddWithValue("addr", emp.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("edu", emp.Education ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("qual", emp.Qualification ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ops", emp.OperationsList ?? (object)DBNull.Value);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("DELETE FROM employees WHERE id_employee = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ==========================================
        // 3. НОВЫЕ МЕТОДЫ ДЛЯ СОЗДАНИЯ/УДАЛЕНИЯ С УЧЕТНОЙ ЗАПИСЬЮ
        // ==========================================

        /// <summary>
        /// Создает нового пользователя и запись о работнике в одной транзакции
        /// </summary>
        public async Task<bool> CreateUserWithEmployeeAsync(
            string login,
            string password,
            int roleId,
            string fullName,
            DateTime birthDate,
            string? address,
            string? education,
            string? qualification,
            string? operationsList)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Проверяем уникальность логина
                await using var checkCmd = new NpgsqlCommand(
                    "SELECT 1 FROM users WHERE login = @login", conn, transaction);
                checkCmd.Parameters.AddWithValue("login", login);

                if (await checkCmd.ExecuteScalarAsync() != null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Хешируем пароль
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                // Создаем пользователя и получаем его ID
                await using var userCmd = new NpgsqlCommand(
                    "INSERT INTO users (login, password, role_id, full_name) VALUES (@login, @password, @role_id, @full_name) RETURNING id_user",
                    conn, transaction);
                userCmd.Parameters.AddWithValue("login", login);
                userCmd.Parameters.AddWithValue("password", hashedPassword);
                userCmd.Parameters.AddWithValue("role_id", roleId);
                userCmd.Parameters.AddWithValue("full_name", fullName);

                var userId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());

                // Создаем запись о работнике
                await using var empCmd = new NpgsqlCommand(
                    "INSERT INTO employees (id_user, birth_date, address, education, qualification, operations_list) VALUES (@id_user, @birth_date, @address, @education, @qualification, @operations_list)",
                    conn, transaction);
                empCmd.Parameters.AddWithValue("id_user", userId);
                empCmd.Parameters.AddWithValue("birth_date", birthDate);
                empCmd.Parameters.AddWithValue("address", address ?? (object)DBNull.Value);
                empCmd.Parameters.AddWithValue("education", education ?? (object)DBNull.Value);
                empCmd.Parameters.AddWithValue("qualification", qualification ?? (object)DBNull.Value);
                empCmd.Parameters.AddWithValue("operations_list", operationsList ?? (object)DBNull.Value);

                await empCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// Удаляет работника вместе с его учетной записью пользователя в одной транзакции
        /// </summary>
        public async Task<bool> DeleteEmployeeWithUserAsync(int employeeId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Получаем id_user связанного пользователя
                await using var getUserIdCmd = new NpgsqlCommand(
                    "SELECT id_user FROM employees WHERE id_employee = @id", conn, transaction);
                getUserIdCmd.Parameters.AddWithValue("id", employeeId);

                var userIdObj = await getUserIdCmd.ExecuteScalarAsync();
                if (userIdObj == null || userIdObj == DBNull.Value)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                var userId = Convert.ToInt32(userIdObj);

                // Сначала удаляем запись о работнике (из-за внешнего ключа)
                await using var deleteEmpCmd = new NpgsqlCommand(
                    "DELETE FROM employees WHERE id_employee = @id", conn, transaction);
                deleteEmpCmd.Parameters.AddWithValue("id", employeeId);
                await deleteEmpCmd.ExecuteNonQueryAsync();

                // Затем удаляем пользователя
                await using var deleteUserCmd = new NpgsqlCommand(
                    "DELETE FROM users WHERE id_user = @id", conn, transaction);
                deleteUserCmd.Parameters.AddWithValue("id", userId);
                await deleteUserCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}