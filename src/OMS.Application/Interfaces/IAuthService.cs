using OMS.Application.DTOs;

namespace OMS.Application.Interfaces {
    public interface IAuthService {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);
    }
}
