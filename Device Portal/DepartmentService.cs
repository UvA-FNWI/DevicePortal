using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevicePortal
{   
    public class DepartmentService
    {
        private readonly IHttpClientFactory _clientFactory;

        public DepartmentService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<Department[]> GetDepartments(string userId) 
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync($"https://api.datanose.nl/Employee/{userId}");
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<Department[]>(responseStream);
            }
            else { return Array.Empty<Department>(); }
        }

        public class Department
        {
            public string Name { get; set; }
            public bool IsManager { get; set; }
        }
    }
}
