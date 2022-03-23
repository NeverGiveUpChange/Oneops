using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OneOpsClient.Api.EF_Sqlite;
using OneOpsClient.Api.Model;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OneOpsClient.Api.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        readonly OneopsContext oneopsContext;
        readonly IHttpContextAccessor httpContextAccessor;
        public UserController(OneopsContext oneopsContext, IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.oneopsContext = oneopsContext;
        }
        /// <summary>
        /// 获取token
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <returns></returns>
        [HttpPost("token")]
        [AllowAnonymous]
        public IActionResult Token([FromForm] LoginInfo loginInfo)
        {

            var userInfo = oneopsContext.UserInfos.Where(x => x.UserName == loginInfo.UserName && x.Password == loginInfo.PassWord && !x.IsDelete).SingleOrDefault();
            if (userInfo != null)
            {

                var token = CreateToken(loginInfo);
                return Ok(new
                {
                    IsSuccess = true,
                    Token = token,
                    Message = "成功获取Token"
                });
            }
            else
            {
                return Ok(new { IsSuccess = false, Message = "用户名或者密码错误", Token = "" });
            }


        }
        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <returns></returns>
        [HttpPost("createuser")]
        [Authorize]
        public IActionResult CreateUser([FromForm] LoginInfo loginInfo)
        {
            var userName = httpContextAccessor.HttpContext.AuthenticateAsync().Result.Principal.Claims.First(x => x.Type.Equals(ClaimTypes.Name))?.Value;
            if (userName != "chenlong")
            {
                return new JsonResult(new { IsSuccess = false, Message = "无权限" });
            }
            if (oneopsContext.UserInfos.Any(x => x.UserName == loginInfo.UserName && !x.IsDelete))
            {
                return new JsonResult(new { IsSuccess = false, Message = "用户名已存在" });
            }
            else
            {
                oneopsContext.UserInfos.Add(new UserInfo { UserName = loginInfo.UserName, Password = loginInfo.PassWord, CreateTime = DateTime.Now, UpdateTime = DateTime.Now, IsDelete = false });
                oneopsContext.SaveChanges();
                return new JsonResult(new { IsSuccess = true, Message = "创建成功" });
            }

        }
        /// <summary>
        /// 获取用户列表
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [HttpGet("userlist")]
        [Authorize]
        public IActionResult UserList([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                var userinfoList = oneopsContext.UserInfos.Where(x => !x.IsDelete).Select(x => new { Id = x.Id, Username = x.UserName, Createtime = x.CreateTime.ToString("yyyy-MM-dd HH:mm:ss") });
                var result = new { Code = 0, Msg = "", Count = userinfoList.Count(), Data = userinfoList };
                return new JsonResult(result);
            }
            else
            {
                var userinfoList = oneopsContext.UserInfos.Where(x => !x.IsDelete && x.UserName == username).Select(x => new { Id = x.Id, UserName = x.UserName, CreateTime = x.CreateTime.ToString("yyyy-MM-dd HH:mm:ss") });
                var result = new { Code = 0, Msg = "", Count = userinfoList.Count(), Data = userinfoList };
                return new JsonResult(result);
            }
        }
        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("delete/{id}")]
        [Authorize]
        public JsonResult DeleteUser([FromRoute] int id)
        {
            var userName = httpContextAccessor.HttpContext.AuthenticateAsync().Result.Principal.Claims.First(x => x.Type.Equals(ClaimTypes.Name))?.Value;
            if (userName != "chenlong")
            {
                return new JsonResult(new { IsSuccess = false, Message = "无权限" });
            }
            var userinfo = oneopsContext.UserInfos.SingleOrDefault(x => x.Id == id);
            if (userinfo != null)
            {
                oneopsContext.UserInfos.Remove(userinfo);
                oneopsContext.SaveChanges();
            }
            return new JsonResult(new { IsSuccess = true, Message = "删除成功" });
        }
        [NonAction]
        public string CreateToken(LoginInfo loginInfo)
        {
            var claims = new[] {
                          new Claim(JwtRegisteredClaimNames.Nbf,$"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}") ,
                          new Claim (JwtRegisteredClaimNames.Exp,$"{new DateTimeOffset(DateTime.Now.AddHours(12)).ToUnixTimeSeconds()}"),
                          new Claim(ClaimTypes.Name, loginInfo.UserName),
                          new Claim(ClaimTypes.Role,loginInfo.Env)
                       };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("A1D0EDC7-A10D-EBC8-FAF9-64710A78FA09"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
           issuer: "oneops.user",
           audience: "oneops.Audience",
           claims: claims,
           expires: DateTime.Now.AddHours(12),
           signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);

        }
    }
}
