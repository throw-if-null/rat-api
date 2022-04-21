﻿CREATE TABLE [dbo].[Member]
(
	[Id] INT NOT NULL IDENTITY(1, 1),
	[AuthProviderId] NVARCHAR(128) NOT NULL,

    [Created] DATETIMEOFFSET NOT NULL DEFAULT  GETUTCDATE(), 
    [Modified] DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(), 
    [Deleted] DATETIMEOFFSET NULL, 
    CONSTRAINT [PK_User_Id] PRIMARY KEY ([Id] ASC),
	CONSTRAINT [UQ_AuthProviderId] UNIQUE ([AuthProviderId])
)
