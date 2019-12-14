SELECT Ev.name, startDate, endDate, Em.fullName, totalAttendees, description FROM modestomovesdb.events as Ev, modestomovesdb.employees as Em
	WHERE Ev.employeeID = Em.employeeID;
