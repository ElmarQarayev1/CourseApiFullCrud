using System;
using System.Drawing;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Course.Ui.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Net.Http.Headers;

namespace Course.Ui.Controllers
{
	public class StudentController:Controller
	{
        private HttpClient _client;
        public StudentController()
        {
            _client = new HttpClient();
        }
        public async Task<IActionResult> Index(int page = 1, int size = 4)
        {
            var token = Request.Cookies["token"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }
            _client.DefaultRequestHeaders.Add(HeaderNames.Authorization,token);
            var queryString = new StringBuilder();
            queryString.Append("?page=").Append(Uri.EscapeDataString(page.ToString()));
            queryString.Append("&size=").Append(Uri.EscapeDataString(size.ToString()));

            string requestUrl = "https://localhost:7064/api/students" + queryString;
            using (var response = await _client.GetAsync(requestUrl))
            {
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<PaginatedResponseResource<StudentListItemGetResponse>>(await response.Content.ReadAsStringAsync(), options);
                    if (data.TotalPages < page) return RedirectToAction("index", new { page = data.TotalPages });

                    return View(data);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("login", "account");
                }
                else
                {
                    return RedirectToAction("error", "home");
                }
            }
        }
        public async Task<IActionResult> Create()
        {
            var token = Request.Cookies["token"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }
            _client.DefaultRequestHeaders.Add(HeaderNames.Authorization,token);

            List<StudentCreateWithGroupRequest> groups = new List<StudentCreateWithGroupRequest>();
            var response = await _client.GetAsync("https://localhost:7064/api/Groups/all");

            if (response.IsSuccessStatusCode)
            {
                groups = await response.Content.ReadFromJsonAsync<List<StudentCreateWithGroupRequest>>();
            }
            else
            {
                TempData["Error"] = "Could not load groups.";
            }

            ViewBag.Groups = new SelectList(groups, "Id", "No");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] StudentCreateRequest createRequest)
         {
            var token = Request.Cookies["token"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (!ModelState.IsValid)
            {
                await PopulateGroupsAsync();
                return View(createRequest);
            }

            try
            {
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(createRequest.FullName), "FullName");
                    content.Add(new StringContent(createRequest.Email), "Email");
                    content.Add(new StringContent(createRequest.BirthDate.ToString("o")), "BirthDate");
                    content.Add(new StreamContent(createRequest.FileName.OpenReadStream()), "FileName", createRequest.FileName.FileName);
                    content.Add(new StringContent(createRequest.GroupId.ToString()), "GroupId");

                    using (HttpResponseMessage response = await _client.PostAsync("https://localhost:7064/api/Students", content))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return RedirectToAction("Index");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            return RedirectToAction("Login", "Account");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(await response.Content.ReadAsStringAsync(), options);

                            foreach (var item in errorResponse.Errors)
                                ModelState.AddModelError(item.Key, item.Message);

                            await PopulateGroupsAsync();
                            return View(createRequest);
                        }
                        else
                        {
                            TempData["Error"] = "Something went wrong";
                            await PopulateGroupsAsync();
                            return View(createRequest);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Exception: {ex.Message}";
                await PopulateGroupsAsync();
                return View(createRequest);
            }
        }

        private async Task PopulateGroupsAsync()
        {
            var response = await _client.GetAsync("https://localhost:7064/api/Groups/all");
            if (response.IsSuccessStatusCode)
            {
                var groups = await response.Content.ReadFromJsonAsync<List<StudentCreateWithGroupRequest>>();
                ViewBag.Groups = new SelectList(groups, "Id", "No");
            }
            else
            {
                ViewBag.Groups = new SelectList(new List<StudentCreateWithGroupRequest>(), "Id", "No");
            }
        }
    
        public async Task<IActionResult> Edit(int id)
        {
            var token = Request.Cookies["token"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"https://localhost:7064/api/students/{id}");
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                var student = JsonSerializer.Deserialize<StudentCreateRequest>(await response.Content.ReadAsStringAsync(), options);
                await PopulateGroupsAsync();
                return View(student);
            }
            return RedirectToAction("Error", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [FromForm] StudentCreateRequest editRequest)
        {
            var token = Request.Cookies["token"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (!ModelState.IsValid)
            {
                await PopulateGroupsAsync();
                return View(editRequest);
            }
            try
            {
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(editRequest.FullName), "FullName");
                    content.Add(new StringContent(editRequest.Email), "Email");
                    content.Add(new StringContent(editRequest.BirthDate.ToString("o")), "BirthDate");
                    content.Add(new StreamContent(editRequest.FileName.OpenReadStream()), "FileName", editRequest.FileName.FileName);
                    content.Add(new StringContent(editRequest.GroupId.ToString()), "GroupId");

                    using (HttpResponseMessage response = await _client.PutAsync($"https://localhost:7064/api/Students/{id}", content))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return RedirectToAction("Index");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            return RedirectToAction("Login", "Account");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(await response.Content.ReadAsStringAsync(), options);

                            foreach (var item in errorResponse.Errors)
                                ModelState.AddModelError(item.Key, item.Message);

                            await PopulateGroupsAsync();
                            return View(editRequest);
                        }
                        else
                        {
                            TempData["Error"] = "Something went wrong";
                            await PopulateGroupsAsync();
                            return View(editRequest);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Exception: {ex.Message}";
                await PopulateGroupsAsync();
                return View(editRequest);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var token = Request.Cookies["token"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _client.DeleteAsync($"https://localhost:7064/api/students/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    TempData["Error"] = "Something went wrong";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Exception: {ex.Message}";
                return RedirectToAction("Index");
            }
        }




    }
}

