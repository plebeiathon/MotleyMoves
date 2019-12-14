using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace MotleyMoves {
    public static class fa_MotleyMoves {
        [FunctionName ("GetRacePoints")]
        public static async Task<string> GetRacePoints ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getRacePoints")] HttpRequest req, ILogger log) {
            try {
                List<string> return_results = new List<string> ();
                string[] fields;

                foreach (string row in CRUDHelper.getQuery ( //Get Race points and points-of-interest
                        String.Format (
                            "SELECT lat, long, pointType FROM modestomovesdb.eventmap WHERE name = '{0}'",
                            req.Query["name"]
                        )
                    )) {
                    fields = row.Split (',');
                    return_results.Add (
                        String.Format (
                            "{{\"lat\": {0}, \"lng\": {1}, \"type\": \"{2}\" }}",
                            fields[0], fields[1], fields[2]
                        )
                    );
                }
                foreach (string row in CRUDHelper.getQuery ( //Get user's positions who are checked into race
                        String.Format (
                            "SELECT xCoord, yCoord FROM modestomovesdb.eventattendees WHERE eventID = {0} AND xCoord IS NOT NULL AND yCoord IS NOT NULL",
                            getEventID (req.Query["name"])
                        )
                    )) {
                    fields = row.Split (',');
                    return_results.Add (
                        String.Format (
                            "{{\"lat\": {0}, \"lng\": {1}, \"type\": \"{2}\" }}",
                            fields[0], fields[1], "Person" //** TODO: replace "person" with what type of person they are (look-up google id and check if mentor, coach (requires same "Auth" endpoint as enabling editting of events)) **//
                        )
                    );
                }

                return String.Format (
                    "{{'points' : [{0}]}}".Replace ("'", "\""),
                    String.Join (",", return_results.ToArray ())
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("PostRacePoints")]
        public static async Task<string> PostRacePoints ([HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = "postRacePoints")] HttpRequest req, ILogger log) {
            try {
                int eventID;
                dynamic body = await GetBody (req.Body);
                string nameID = String.Join ('-', body.name.ToString ().ToLower ().Split (' '));
                int event_count = CRUDHelper.executeScalar (
                    String.Format (
                        "SELECT COUNT(*) FROM modestomovesdb.raceEvents WHERE nameID = '{0}'",
                        nameID
                    )
                );
                if (event_count > 0) {
                    eventID = int.Parse (
                        CRUDHelper.getQuery (
                            String.Format (
                                "SELECT eventID FROM modestomovesdb.raceEvents WHERE nameID = '{0}'",
                                nameID
                            )
                        ) [0]);
                    Console.WriteLine (
                        CRUDHelper.executeNonQuery (
                            String.Format (
                                "UPDATE modestomovesdb.raceEvents SET startDate = '{0}', description = '{1}' WHERE eventID = {2}",
                                body.date,
                                body.description.ToString ().Replace (",", "~~~"),
                                eventID
                            )
                        )
                    );
                } else {
                    eventID = int.Parse ( // Get new eventID
                        CRUDHelper.getQuery (
                            "SELECT COUNT(*) FROM modestomovesdb.raceEvents"
                        ) [0]
                    ) + 1;
                    Console.WriteLine ( // Insert Values into raceEvent
                        CRUDHelper.executeNonQuery (
                            String.Format (
                                "INSERT INTO modestomovesdb.raceEvents VALUES ('{0}', '{1}', '{2}', '{3}')",
                                // eventID,
                                body.name,
                                body.date,
                                body.description.ToString ().Replace (",", "~~~"),
                                nameID
                            )
                        )
                    );
                }
                if (body.points.Count != 0) { //Delete Points
                    CRUDHelper.executeNonQuery (
                        String.Format (
                            "DELETE FROM modestomovesdb.eventmap WHERE name = '{0}'",
                            nameID
                        )
                    );

                }
                return event_count + " " + CRUDHelper.executeNonQuery ( //Insert Points
                    String.Format (
                        "INSERT INTO modestomovesdb.eventmap VALUES {0}",
                        String.Join (
                            ",",
                            ((IEnumerable<dynamic>) body.points).Select (
                                point => "(" + eventID + "," + point["lat"] + "," + point["lng"] + ",'" + nameID + "','" + point["type"] + "')"
                            )
                        )
                    )
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("GetCalendarEvents")]
        public static async Task<string> GetCalendarEvents ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getCalendarEvents")] HttpRequest req, ILogger log) {
            try {
                return String.Format ( //Generate the member object
                    "{{'events' : [{0}]}}".Replace ("'", "\""),
                    String.Join (
                        ",",
                        CRUDHelper.getQuery (
                            "SELECT name, startDate, description FROM modestomovesdb.raceEvents"
                        ).Select (
                            x => "{" + CRUDHelper.formatEvent (x.Split (',')) + "}"
                        ).ToArray ()
                    )
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("GetAuth")]
        public static async Task<string> GetAuth ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getAuth")] HttpRequest req, ILogger log) {
            try {
                // Check if google id is in admin table or participant table...

                return "Done: " + CRUDHelper.executeNonQuery (
                    "DELETE FROM modestomovesdb.employees"
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("SetAuth")]
        public static async Task<string> SetAuth ([HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = "setAuth")] HttpRequest req, ILogger log) {
            try {
                // move google id to admin table from participant table...
                // int employeeID = int.Parse ( // Get new eventID
                //     CRUDHelper.getQuery (
                //         "SELECT COUNT(*) FROM modestomovesdb.employees"
                //     ) [0]
                // ) + 1;
                return "Success: " + String.Format (
                    "INSERT INTO modestomovesdb.employees VALUES ('{0}', 'admin')",
                    req.Query["googleID"]
                ) + " " + CRUDHelper.executeNonQuery (
                    String.Format (
                        "INSERT INTO modestomovesdb.employees VALUES ('{0}', 'admin')",
                        req.Query["googleID"]
                    )
                );

            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("GetEvent")]
        public static async Task<string> GetEvent ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getEvent")] HttpRequest req, ILogger log) {
            try {
                List<string> output = CRUDHelper.getQuery (
                    String.Format (
                        "SELECT name, startDate, description FROM modestomovesdb.raceEvents WHERE nameID = '{0}'",
                        req.Query["name"]
                    )
                );
                string is_ongoing = "false";

                var utc = DateTime.UtcNow;
                TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById ("Pacific Standard Time");
                var pacificTime = TimeZoneInfo.ConvertTimeFromUtc (utc, pacificZone);

                var format = "yyyy-MM-dd";
                var stringDate = pacificTime.ToString (format);

                DateTime date_time = DateTime.Parse (output[0].Split (",") [1]);
                string formattedDate = date_time.ToString (format);

                if (formattedDate == stringDate) {
                    is_ongoing = "true";
                }
                return String.Format ( //Generate the member object
                    "{{{0}, \"is_ongoing\": \"{1}\" }}",
                    CRUDHelper.formatEvent (output[0].Split (",")),
                    is_ongoing
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("DeleteEvent")]
        public static async Task<string> DeleteEvent ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "deleteEvent")] HttpRequest req, ILogger log) {
            try {
                return CRUDHelper.executeNonQuery ( // Delete an event in the database
                    String.Format (
                        "DELETE FROM modestomovesdb.raceEvents WHERE nameID = '{0}'",
                        req.Query["name"]
                    )
                ) + " Deleted " + req.Query["name"];
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("GetMember")]
        public static async Task<string> GetMember ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getMember")] HttpRequest req, ILogger log) {
            try {
                if (CRUDHelper.getQuery (
                        String.Format (
                            "SELECT COUNT(*) FROM modestomovesdb.members WHERE googleID = {0}",
                            req.Query["googleID"]
                        )
                    ) [0] != "1") {
                    CRUDHelper.executeNonQuery ( // Create a new entry in the database
                        String.Format (
                            "INSERT INTO modestomovesdb.members (googleID, lastName, firstName, email) VALUES ('{0}', '{1}', '{2}', '{3}')",
                            req.Query["googleID"],
                            req.Query["lastName"],
                            req.Query["firstName"],
                            req.Query["email"]
                        )
                    );
                }
                string member_info = CRUDHelper.getQuery ( // Run get query from the DB
                    String.Format (
                        "SELECT * FROM modestomovesdb.members WHERE googleID = {0}",
                        req.Query["googleID"]
                    )
                ) [0];
                string debug = "";
                if (CRUDHelper.getQuery (
                        String.Format (
                            "SELECT COUNT(*) FROM modestomovesdb.medicalinfo WHERE memberID = {0}",
                            member_info.Split (',') [0]
                        )
                    ) [0] != "1") {
                    debug = "insert medical: " + CRUDHelper.executeNonQuery (
                        String.Format (
                            @"INSERT INTO modestomovesdb.medicalinfo VALUES ({0},{1},'{2}','{3}',{4},'{5}','{6}','{7}','{8}','{9}',{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},'{22}')",
                            member_info.Split (',') [0], 21, "", "", 120, "", "", "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ""
                        )
                    );
                }

                string medical_info = CRUDHelper.getQuery (
                    String.Format (
                        "SELECT * FROM modestomovesdb.medicalinfo WHERE memberID = {0}",
                        member_info.Split (',') [0]
                    )
                ) [0];

                int is_admin = int.Parse (
                    CRUDHelper.getQuery (
                        String.Format (
                            "SELECT COUNT(*) FROM modestomovesdb.employees WHERE googleID = {0}",
                            req.Query["googleID"]
                        )
                    ) [0]
                );
                return String.Format (
                    "{{ \"member_info\": \"{0}\", \"medical_info\": \"{1}\", \"is_admin\": {2}, \"debug\": \"{3}\" }}",
                    member_info,
                    medical_info,
                    is_admin,
                    debug
                );
            } catch (Exception ex) {
                return ex.ToString () + " " + CRUDHelper.getQuery (
                    String.Format (
                        "SELECT * FROM modestomovesdb.members WHERE googleID = {0}",
                        req.Query["googleID"]
                    )
                ) [0];
            }
        }

        [FunctionName ("GetMembers")]
        public static async Task<string> GetMembers ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getMembers")] HttpRequest req, ILogger log) {
            try {
                string result = "";
                foreach (string row in CRUDHelper.getQuery ("SELECT * FROM modestomovesdb.members")) {
                    result += row + "\n";
                }
                return result;
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("GetEmployees")]
        public static async Task<string> GetEmployees ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getEmployees")] HttpRequest req, ILogger log) {
            try {
                string result = "";
                foreach (string row in CRUDHelper.getQuery ("SELECT googleID FROM modestomovesdb.members")) {
                    if (
                        CRUDHelper.getQuery ("SELECT COUNT(*) FROM modestomovesdb.employees where googleID = " + row) [0] == "1"
                    ) {
                        result += CRUDHelper.getQuery ("SELECT * FROM modestomovesdb.members where googleID = " + row) [0] + ",";
                        result += CRUDHelper.getQuery ("SELECT * FROM modestomovesdb.employees where googleID = " + row) [0] + "\n";
                    }
                }
                return result;
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("GetEventMember")]
        public static async Task<string> GetEventMember ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getEventMember")] HttpRequest req, ILogger log) {
            try {
                string checked_in = "";
                string checked_out = "";
                if (checkUser ("modestomovesdb.members", "googleID", req.Query["googleID"]) == false) {
                    CRUDHelper.executeNonQuery ( // Create a new entry in the database
                        String.Format (
                            "INSERT INTO modestomovesdb.members (googleID, lastName, firstName, email) VALUES ('{0}', '{1}', '{2}', '{3}')",
                            req.Query["googleID"],
                            req.Query["lastName"],
                            req.Query["firstName"],
                            req.Query["email"]
                        )
                    );
                }
                int memberID = int.Parse (
                    CRUDHelper.getQuery (
                        String.Format (
                            "SELECT memberID FROM modestomovesdb.members WHERE googleID = {0}",
                            req.Query["googleID"]
                        )
                    ) [0]
                );
                CRUDHelper.executeNonQuery (
                    String.Format (
                        @"INSERT INTO modestomovesdb.medicalinfo VALUES ({0},{1},'{2}','{3}',{4},'{5}','{6}','{7}','{8}','{9}',{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},'{22}')",
                        memberID, 21, "", "", 120, "", "", "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "Something witty!"
                    )
                );
                int has_registered = int.Parse (
                    CRUDHelper.getQuery (
                        String.Format (
                            "SELECT COUNT(*) FROM modestomovesdb.eventAttendees WHERE memberID = {0} AND eventID = {1}",
                            memberID,
                            getEventID (req.Query["eventID"])
                        )
                    ) [0]
                );
                if (has_registered == 1) {
                    checked_in = CRUDHelper.getQuery (
                        String.Format (
                            "SELECT checkIn FROM modestomovesdb.eventAttendees WHERE memberID = {0} AND eventID = {1}",
                            memberID,
                            getEventID (req.Query["eventID"])
                        )
                    ) [0];
                    checked_out = CRUDHelper.getQuery (
                        String.Format (
                            "SELECT checkOut FROM modestomovesdb.eventAttendees WHERE memberID = {0} AND eventID = {1}",
                            memberID,
                            getEventID (req.Query["eventID"])
                        )
                    ) [0];
                }
                int is_admin = int.Parse (
                    CRUDHelper.getQuery (
                        String.Format (
                            "SELECT COUNT(*) FROM modestomovesdb.employees WHERE googleID = {0}",
                            req.Query["googleID"]
                        )
                    ) [0]
                );

                return String.Format (
                    "{{ \"has_registered\": \"{0}\", \"checked_in\": \"{1}\", \"checked_out\": \"{2}\", \"is_admin\": \"{3}\" }}",
                    has_registered,
                    checked_in,
                    checked_out,
                    is_admin
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("UpdateUser")]
        public static async Task<string> UpdateUser ([HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = "updateUser")] HttpRequest req, ILogger log) {
            try {
                dynamic body = await GetBody (req.Body);

                int memberID = getMemberID (body.googleID.ToString ());

                string debug = "";
                if (CRUDHelper.getQuery (
                        String.Format (
                            "SELECT COUNT(*) FROM modestomovesdb.medicalinfo WHERE memberID = {0}",
                            memberID
                        )
                    ) [0] == "0") {
                    debug = "insert medical: " + CRUDHelper.executeNonQuery (
                        String.Format (
                            @"INSERT INTO modestomovesdb.medicalinfo VALUES ({0},{1},'{2}','{3}',{4},'{5}','{6}','{7}','{8}','{9}',{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},'{22}')",
                            memberID, 21, "", "", 120, "", "", "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ""
                        )
                    );
                }

                return debug + " " + " Update User Success: medicalprov " + CRUDHelper.executeNonQuery ( // Update Medical Table
                    String.Format (
                        "UPDATE modestomovesdb.medicalinfo SET medicalProvider = '{1}', allergiesToMedication = '{2}', currentMedication = '{3}' WHERE memberID = {0}",
                        memberID, body.provider, body.allergies, body.medications
                    )
                ) + " high blood pressure " + CRUDHelper.executeNonQuery ( // Update Medical Table
                    String.Format (
                        "UPDATE modestomovesdb.medicalinfo SET highBloodPressure = {1}, heartDisease = {2}, heartAttack = {3}, stroke = {4}, vascularDisease = {5}, neuromuscularDisease = {6}, kidneyRenalDisease = {7}, bariatricSurgery = {8}, arthritis = {9}, cancer = {10}, congenitalDisease = {11}, psychiatricDisorder = {12} WHERE memberID = {0}",
                        memberID, body.pressure, body.heart, body.heartattack, body.stroke, body.vascular, body.neuromuscular, body.kidney, body.bariatric, body.arthritis, body.cancer, body.congenital, body.psychiatric
                    )
                ) + " other " + body.otherConditions + CRUDHelper.executeNonQuery ( // Update Medical Table
                    String.Format (
                        "UPDATE modestomovesdb.medicalinfo SET otherConditions = '{1}' WHERE memberID = {0}",
                        memberID, body.otherConditions
                    )
                ) + " phone " + body.emergtel + CRUDHelper.executeNonQuery ( // Update Medical Table
                    String.Format (
                        "UPDATE modestomovesdb.medicalinfo SET ecPhone = '{1}' WHERE memberID = {0}",
                        memberID, body.emergtel
                    )
                ) + " name " + body.emergname + CRUDHelper.executeNonQuery ( // Update Medical Table
                    String.Format (
                        "UPDATE modestomovesdb.medicalinfo SET ecName = '{1}' WHERE memberID = {0}",
                        memberID, body.emergname
                    )
                ) + " age and weight " + body.age + body.weight + CRUDHelper.executeNonQuery ( // Update Medical Table
                    String.Format (
                        "UPDATE modestomovesdb.medicalinfo SET age = {1}, weight = {2} WHERE memberID = {0}",
                        memberID, body.age, body.weight
                    )
                ) + " height " + body.height + CRUDHelper.executeNonQuery ( // Update Medical Table
                    String.Format (
                        "UPDATE modestomovesdb.medicalinfo SET height = '{1}' WHERE memberID = {0}",
                        memberID, body.height
                    )
                ) + " gender " + body.gender + CRUDHelper.executeNonQuery ( // Update Medical Table
                    String.Format (
                        "UPDATE modestomovesdb.medicalinfo SET gender = '{1}' WHERE memberID = {0}",
                        memberID, body.gender
                    )
                ) + " phone " + CRUDHelper.executeNonQuery ( // Update Member Table
                    String.Format (
                        "UPDATE modestomovesdb.members SET phone = '{1}', bio = '{2}' WHERE googleID = '{0}'",
                        body.googleID,
                        body.tel,
                        body.biography
                    )
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        /* * * * * * * * * * * * * * * * */
        /* Event + Participant Endpoints */
        /* * * * * * * * * * * * * * * * */
        [FunctionName ("RegisterForEvent")]
        public static async Task<string> RegisterForEvent ([HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = "registerForEvent")] HttpRequest req, ILogger log) {
            try {

                CRUDHelper.executeNonQuery (
                    String.Format (
                        "INSERT INTO modestomovesdb.eventattendees (eventID, memberID) VALUES ({0}, {1})",
                        getEventID (req.Query["eventID"]),
                        getMemberID (req.Query["googleID"])
                    )
                );
                return "Register Success";
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("GetEventAttendees")]
        public static async Task<string> GetEventAttendees ([HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = "getEventAttendees")] HttpRequest req, ILogger log) {
            try {
                List<string> return_results = new List<string> ();
                string[] fields;

                foreach (string member in CRUDHelper.getQuery (
                        String.Format (
                            "SELECT memberID FROM modestomovesdb.eventattendees WHERE eventID = '{0}'",
                            getEventID (req.Query["eventID"])
                        )
                    )) {
                    //Could also get the Checkin and Checkout dates here...

                    foreach (string row in CRUDHelper.getQuery ( //Get Race points and points-of-interest
                            String.Format (
                                //UPDATE modestomovesdb.members SET lastName = '{1}', firstName = '{2}', email = '{3}', phone = '{4}'
                                "SELECT firstName, lastName, email, phone FROM modestomovesdb.members WHERE memberId = '{0}'",
                                member
                            )
                        )) {
                        fields = row.Split (',');

                        string[] event_fields = CRUDHelper.getQuery (
                            String.Format (
                                "SELECT checkIn, checkOut FROM modestomovesdb.eventattendees WHERE eventID = '{0}'",
                                getEventID (req.Query["eventID"])
                            )
                        ) [0].Split (',');

                        string medical_fields = CRUDHelper.getQuery (
                            String.Format (
                                "SELECT * FROM modestomovesdb.medicalinfo WHERE memberID = {0}",
                                member
                            )
                        ) [0];
                        return_results.Add (
                            String.Format (
                                "{{\"firstName\": \"{0}\", \"lastName\": \"{1}\", \"email\": \"{2}\", \"phone\": \"{3}\", \"checkIn\": \"{4}\", \"checkOut\": \"{5}\", \"medical_info\": \"{6}\" }}",
                                fields[0],
                                fields[1],
                                fields[2],
                                fields[3],
                                event_fields[0],
                                event_fields[1],
                                medical_fields
                            )
                        );
                    }
                }
                return String.Format (
                    "{{'members' : [{0}]}}".Replace ("'", "\""),
                    String.Join (",", return_results.ToArray ())
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("CheckIn")]
        public static async Task<string> CheckIn ([HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = "checkIn")] HttpRequest req, ILogger log) {
            try {

                var utc = DateTime.UtcNow;
                TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById ("Pacific Standard Time");
                var pacificTime = TimeZoneInfo.ConvertTimeFromUtc (utc, pacificZone);

                var format = "yyyy-MM-dd HH:mm:ss:fff";
                var stringDate = pacificTime.ToString (format);
                var convertedBack = DateTime.ParseExact (stringDate, format, CultureInfo.InvariantCulture);

                return "Checked In: " + CRUDHelper.executeNonQuery (
                    String.Format (
                        "UPDATE modestomovesdb.eventattendees SET checkIn = '{2}' WHERE eventID = {0} AND memberID = {1}",
                        getEventID (req.Query["eventID"]),
                        getMemberID (req.Query["googleID"]),
                        convertedBack
                    )
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("CheckOut")]
        public static async Task<string> CheckOut ([HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = "checkOut")] HttpRequest req, ILogger log) {
            try {
                var utc = DateTime.UtcNow;
                TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById ("Pacific Standard Time");
                var pacificTime = TimeZoneInfo.ConvertTimeFromUtc (utc, pacificZone);

                var format = "yyyy-MM-dd HH:mm:ss:fff";
                var stringDate = pacificTime.ToString (format);
                var convertedBack = DateTime.ParseExact (stringDate, format, CultureInfo.InvariantCulture);

                return "Checked Out: " + CRUDHelper.executeNonQuery (
                    String.Format (
                        "UPDATE modestomovesdb.eventattendees SET checkOut = '{2}' WHERE eventID = {0} AND memberID = {1}",
                        getEventID (req.Query["eventID"]),
                        getMemberID (req.Query["googleID"]),
                        convertedBack
                    )
                );
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        [FunctionName ("UpdatePosition")]
        public static async Task<string> UpdatePosition ([HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = "updatePosition")] HttpRequest req, ILogger log) {
            try {
                return CRUDHelper.executeNonQuery (
                    String.Format ( /* double check */
                        "UPDATE modestomovesdb.eventattendees SET xCoord = {0}, yCoord = {1} WHERE eventID = {2} AND memberID = {3}",
                        req.Query["lat"],
                        req.Query["lng"],
                        getEventID (req.Query["eventID"]),
                        getMemberID (req.Query["googleID"])
                    )
                ) + " Changed. " + req.Query["lat"] + " , " + req.Query["lng"] + ", " + getEventID (req.Query["eventID"]) + ", " + getMemberID (req.Query["googleID"]);
            } catch (Exception ex) {
                return ex.ToString ();
            }
        }

        /* * * * * * * * * * */
        /* Helping Functions */
        /* * * * * * * * * * */
        public static bool checkUser (string table, string condition, string id) { // Check if the Google ID exists in the DB
            return CRUDHelper.getQuery (
                String.Format (
                    "SELECT * FROM {0} WHERE {1} = '{2}';",
                    table,
                    condition,
                    id
                )
            ).Count != 0; //true if user exists
        }
        public static int getEventID (string eventName) { // Gets eventID with the name of the event
            return int.Parse (CRUDHelper.getQuery (
                String.Format (
                    "SELECT eventID FROM modestomovesdb.raceEvents WHERE nameID = '{0}'",
                    eventName
                )
            ) [0]);
        }
        public static int getMemberID (string googleID) { // Gets member ID
            List<string> query_result = CRUDHelper.getQuery (
                String.Format (
                    "SELECT memberID FROM modestomovesdb.members WHERE googleID = '{0}'",
                    googleID
                )
            );
            return int.Parse (query_result[0]);
        }
        public static async Task<dynamic> GetBody (Stream body) { // Assembles HTTP Body byte-stream into JSON
            using (var reader = new StreamReader (body)) {
                return Newtonsoft.Json.JsonConvert.DeserializeObject (await reader.ReadToEndAsync ());
            }
        }
    }
}