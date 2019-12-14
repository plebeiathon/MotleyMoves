var googleUser = {};
var data = {};
var profile;

var startApp = function () {
    gapi.load('auth2', function () {
        auth2 = gapi.auth2.init({
            client_id: '....apps.googleusercontent.com',
            cookiepolicy: 'single_host_origin',
        });
        attachSignin(document.getElementById("customBtn"));
    });
};

function attachSignin(element) {
    auth2.attachClickHandler(element, {},
        function (googleUser) {
            profile = googleUser.getBasicProfile();
            /* Creates an account for a Google ID if it doesn't exist, and return all known information */
            data = { "googleID": profile.getId(), "firstName": profile.getGivenName(), "lastName=": profile.getFamilyName(), "email": profile.getEmail() };
            $.ajax({
                url: ".../api/getMember" +
                    "?googleID=" + profile.getId() +
                    "&firstName=" + profile.getGivenName() +
                    "&lastName=" + profile.getFamilyName() +
                    "&email=" + profile.getEmail(),
                success: function (result) {
                    var result_json = JSON.parse(result);
                    if (result_json.is_admin) {

                        document.getElementById("view_button").style.display = "block";
                    }

                    document.getElementById("image").src = profile.getImageUrl()
                    document.getElementById("name").textContent = profile.getGivenName().replace(/^\w/, c => c.toUpperCase()) + " " + profile.getFamilyName().replace(/^\w/, c => c.toUpperCase());


                    document.getElementById("signin").style.display = "none";
                    document.getElementById("signedin").style.display = "block";

                    var medical_items = result_json.medical_info.split(",");
                    var member_items = result_json.member_info.split(",");

                    var member_fields = ["last-name", "first-name", "email", "tel", "biography"];
                    for (var i = 0; i < member_fields.length; i++) {
                        if (member_fields[i] == "biography") {
                            document.getElementById("biography").value = member_items[i + 3];
                            document.getElementById("biography-text").value = member_items[i + 3];

                        }
                        else {
                            document.getElementById(member_fields[i]).value = member_items[i + 2];
                        }
                    }
                    var value_fields = ["age", "gender", "height", "weight", "emergname", "emergtel", "provider", "allergies", "medications"];
                    for (var i = 0; i < value_fields.length; i++) {
                        document.getElementById(value_fields[i]).value = medical_items[i + 2];
                    }
                    var check_fields = ["pressure", "heart", "heartattack", "stroke", "vascular", "neuromuscular", "kidney", "bariatric", "arthritis", "cancer", "congenital", "psychiatric"];
                    for (var i = 0; i < check_fields.length; i++) {
                        if (result_json[check_fields[i]] == "true") {
                            document.getElementById(check_fields[i]).checked = bool.Parse(medical_items[i + 11]);
                        }
                    }
                    document.getElementById("otherConditions").value = medical_items[23];
                }
            });
        }, function (error) {
            alert(JSON.stringify(error, undefined, 2));
        }
    );
}

function viewInfo() {
    $.ajax({
        url: ".../api/getMembers",
        success: function (result) {
            attendees = result.split('\n');
			for (var i = 0; i < attendees.length - 1; i++) {
                attendee_fields = attendees[i].split(',');
				$("#attendees").append(
					$.parseHTML(
						`<div class="field">
							<ul class="icons">
								<li>
									<p>` + attendee_fields[3].replace(/^\w/, c => c.toUpperCase()) + " " + attendee_fields[2].replace(/^\w/, c => c.toUpperCase()) + `</p>
								</li>
								<li>
									<a href="mailto:` + attendee_fields[4] + `" class="icon solid style2 major fa-envelope"><span class="label">Email</span></a>
								</li>
								<li>
									<a href="tel:` + attendee_fields[5] + `" class="icon solid style2 major fa-phone"><span class="label">Phone</span></a>
								</li>
							</ul>
						</div>`
					)
				);
			}
        }
    });
}

function updateUser() {
    var value_fields = ["tel", "biography", "emergname", "emergtel", "gender", "age", "weight", "height", "provider", "allergies", "medications", "otherConditions"];
    for (var i = 0; i < value_fields.length; i++) {
        data[value_fields[i]] = document.getElementById(value_fields[i]).value;
    }
    var check_fields = ["pressure", "heart", "heartattack", "stroke", "vascular", "neuromuscular", "kidney", "bariatric", "arthritis", "cancer", "congenital", "psychiatric"];
    for (var i = 0; i < check_fields.length; i++) {
        data[check_fields[i]] = 0;
        if (document.getElementById(check_fields[i]).checked) data[check_fields[i]] = 1;
    }
    data["googleID"] = profile.getId();
    $.post(
        ".../api/updateUser",
        JSON.stringify(data),
        function (result) {
        }
    );
}
startApp();