﻿using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tea.Core.Data;
using Tea.Web.Models;

namespace Tea.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class UserController : BaseController
    {
        private readonly IDataStore _dataStore;

        public UserController(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        [HttpPost]
        [Route("createuser")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(GetModelStateMessages());
            
            if(string.IsNullOrEmpty(model.LocalizedString))
            {
                 model.LocalizedString = HttpContext.Request.GetTypedHeaders()
                .AcceptLanguage.OrderByDescending(x=>x.Quality ?? 0.1).FirstOrDefault()?.Value.ToString()
                ?? "en-GB";
            }         
            
            var user = Core.Domain.User.CreateNewUser(model.LocalizedString, model.Firstname, model.Surname);
            if (!user.SetPassword(model.Password)) return BadRequest("Password is not valid.");
            if (!user.SetEmail(model.EmailAddress)) return BadRequest("Email Address is not valid.");
            user = await _dataStore.CreateAsync(user);

            return Ok(user);
        }

        [HttpPost]
        [Route("updateuser")]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(GetModelStateMessages());

            var user = await _dataStore.GetUserBySimpleIdAsync(model.SimpleId);

            if (user == null)
                return NotFound($"Nothing found for user id {model.SimpleId}");

            model.UpdateUserFromModel(user);
            await _dataStore.UpdateAsync(user);

            return Ok(user);
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound("Please pass a user id");

            var user = await _dataStore.GetUserBySimpleIdAsync(id);

            if (user == null)
                return NotFound($"Nothing found for user id {id}");

            return Ok(user);
        }
    }
}
