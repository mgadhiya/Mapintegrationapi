using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Datalayer.Models.DB;
using StoredProcedureEFCore;
using Datalayer.DTO;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using GoogleMaps.LocationServices;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;

namespace Mapintegration.Controllers
{
    [Produces("application/json")]
    [Route("/[controller]")]
    public class PersonController : Controller
    {
        private readonly mapDbContext  _context;

        public PersonController(mapDbContext context)
        {
            _context = context;
        }

        // GET api/Person/5
      
        [Route("GetPersonDetail/{id}")]
        [EnableCors("SiteCorsPolicy")]
        public IActionResult GetPersonDetail(string id)
        {

            Person person = _context.Person.Where(p => p.PersonId.Contains(id)).FirstOrDefault<Person>();
            return Ok(person);

        }

        // GET api/Person
        [Route("GetAllPerson")]
        [EnableCors("SiteCorsPolicy")]
        public IActionResult GetAllPerson()
        {
            return Ok(_context.Person);

        }
        // GET api/Center
        
        [Route("GetAllCenter")]
        [EnableCors("SiteCorsPolicy")]
        public IActionResult GetAllCenter()
        {
            return Ok(_context.Center);

        }
        // GET api/Zone
        [Route("GetAllZone")]
        [EnableCors("SiteCorsPolicy")]
        public IActionResult GetAllZone()
        {
            return Ok(_context.Zone);

        }
        // GET api/City
        [Route("GetAllCity")]
        [EnableCors("SiteCorsPolicy")]
        public IActionResult GetAllCity()
        {
            return Ok(_context.City);

        }

        // GET api/Person
        [Route("FamilyCount")]
        public async Task<IActionResult> FamilyCountAsync(string address,double dist)
        {

            string url = "http://maps.google.com/maps/api/geocode/json?address=" + address + "&sensor=false";
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            RootObject rootObject = new RootObject();


            HttpClient client = new HttpClient();

            var response = await client.GetAsync(url);


            string result = await response.Content.ReadAsStringAsync();


            rootObject = JsonConvert.DeserializeObject<RootObject>(result, jsonSerializerSettings);

            

            double latitude = 0.0;
            double longitude = 0.0;


            foreach(var item in rootObject.results)
            {

                latitude = item.geometry.location.lat;
                longitude = item.geometry.location.lng;
             
            }


            List<latlongDTO> rows = new List<latlongDTO>();
            // var connection = (SqlConnection)_context.Database.GetDbConnection();
            using (SqlConnection connection = (SqlConnection)_context.Database.GetDbConnection())
            {
                connection.Open();
                   var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_city_family_count";



                command.Parameters.Add("@Latitude", SqlDbType.Float).Value = latitude;
                command.Parameters.Add("@Longitude", SqlDbType.Float).Value = longitude;
                command.Parameters.Add("@distance", SqlDbType.Float).Value = dist;
               
                // execute the command
                using (SqlDataReader rdr = command.ExecuteReader())
                {
                   while(rdr.Read())
                    {
                        latlongDTO objatlongDTO = new latlongDTO();
                        objatlongDTO.city = rdr["city"].ToString();
                        objatlongDTO.distance = Decimal.Parse(rdr["distance"].ToString());
                        objatlongDTO.familycount = (int) rdr["familycount"];
                        objatlongDTO.zipcpde = rdr["zip_code"].ToString();
                        rows.Add(objatlongDTO);
                    }


                }
            }
            return Ok(rows);

        }
        // GET api/Person
        [HttpGet]
        [Route("Latlong")]
        [EnableCors("SiteCorsPolicy")]
       // [Authorize]
        public async Task<IActionResult> LatlongAsync(string address, double dist,string key)
        {
           string key1 = key ?? "AIzaSyBviomBjgDK6492DrGWBU6h0Guwxg9O20A";
            string url = "http://maps.google.com/maps/api/geocode/json?address=" + address + "&sensor=false" + "&key =" + key1;
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            RootObject rootObject = new RootObject();


            HttpClient client = new HttpClient();

            var response = await client.GetAsync(url);


            string result = await response.Content.ReadAsStringAsync();


            rootObject = JsonConvert.DeserializeObject<RootObject>(result, jsonSerializerSettings);



            double latitude = 0.0;
            double longitude = 0.0;
            string location = "";
            string lable = "";

            foreach (var item in rootObject.results)
            {

                latitude = item.geometry.location.lat;
                longitude = item.geometry.location.lng;
                location = item.address_components[2].long_name;
                lable = item.address_components[3].long_name;
            }


            List<latlngDTO> rows = new List<latlngDTO>();
            // var connection = (SqlConnection)_context.Database.GetDbConnection();
            using (SqlConnection connection = (SqlConnection)_context.Database.GetDbConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_lat_long_data";


                command.Parameters.Add("@Latitude", SqlDbType.Float).Value = latitude;
                command.Parameters.Add("@Longitude", SqlDbType.Float).Value = longitude;
                command.Parameters.Add("@distance", SqlDbType.Float).Value = dist;

                // execute the command
                using (SqlDataReader rdr = command.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        latlngDTO objlatlngDTO = new latlngDTO();
                        objlatlngDTO.latitude = Double.Parse(rdr["latitude"].ToString());
                        objlatlngDTO.longitude = Double.Parse(rdr["longtitude"].ToString());
                        objlatlngDTO.count = (int)rdr["count"];
                        objlatlngDTO.location = location;
                        objlatlngDTO.city = rdr["city"].ToString();
                        rows.Add(objlatlngDTO);
                    }


                }
            }
            return Ok(rows);

        }
    }
}