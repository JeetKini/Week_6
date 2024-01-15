select * from LeadTagMapping
order by CreateDate desc

go
select * from Tag
order by CreateDate desc

select * from CRMLead
order by CreatedDate desc

select ltm.leadId, ltm.tagId,t.tagName
from LeadTagMapping ltm left  join Tag t on ltm.tagId=t.id 

--declare @TagIds varchar(200)='109'
 SELECT ltm.Id,ltm.TagId,t.TagName, cl.LeadName,cl.Id,cl.CreatedDate AS LeadCreateDate                      
     FROM LeadTagMapping ltm
	 LEFT JOIN CRMLead cl ON cl.Id=ltm.LeadId
	 LEFT JOIN Tag t ON t.Id=ltm.TagId 
	 WHERE ltm.TagId=109 
--     WHERE TagId IN (              