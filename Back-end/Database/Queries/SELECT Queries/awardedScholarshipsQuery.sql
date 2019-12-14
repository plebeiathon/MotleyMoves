SELECT M.fullName, S.name, dateAwarded FROM modestomovesdb.awardedscholarships as A, modestomovesdb.members as M, modestomovesdb.scholarshiptypes as S
	WHERE A.memberID = M.mermberID AND A.stID = S.stID;
