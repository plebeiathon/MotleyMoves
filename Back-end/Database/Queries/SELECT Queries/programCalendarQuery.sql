SELECT Pg.name, date, totalAttendees, fullName FROM programs as Pg, programcalander as Pc, employees as Em
	WHERE Pg.programID = Pc.programID AND Pc.employeeID = Em.employeeID;
