SELECT M.fullName, Rt.name, dateGiven FROM modestomovesdb.rewards as R, modestomovesdb.members as M, modestomovesdb.rewardtype as Rt
	WHERE R.memberID = M.mermberID AND R.rtID = Rt.rtID;
