namespace Raksha.Application.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        /// <summary>
        /// The expired access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// The current refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }
}
