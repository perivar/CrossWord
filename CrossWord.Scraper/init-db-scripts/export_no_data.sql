
CREATE DATABASE /*!32312 IF NOT EXISTS*/ `dictionary` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */;

USE `dictionary`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AspNetRoleClaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AspNetRoles` (
  `Id` varchar(255) NOT NULL,
  `Name` varchar(256) DEFAULT NULL,
  `NormalizedName` varchar(256) DEFAULT NULL,
  `ConcurrencyStamp` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AspNetUserClaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetUserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AspNetUserLogins` (
  `LoginProvider` varchar(255) NOT NULL,
  `ProviderKey` varchar(255) NOT NULL,
  `ProviderDisplayName` longtext,
  `UserId` varchar(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_AspNetUserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AspNetUserRoles` (
  `UserId` varchar(255) NOT NULL,
  `RoleId` varchar(255) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AspNetUserTokens` (
  `UserId` varchar(255) NOT NULL,
  `LoginProvider` varchar(255) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Value` longtext,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AspNetUsers` (
  `Id` varchar(255) NOT NULL,
  `UserName` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `EmailConfirmed` bit(1) NOT NULL,
  `PasswordHash` longtext,
  `SecurityStamp` longtext,
  `ConcurrencyStamp` longtext,
  `PhoneNumber` longtext,
  `PhoneNumberConfirmed` bit(1) NOT NULL,
  `TwoFactorEnabled` bit(1) NOT NULL,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` bit(1) NOT NULL,
  `AccessFailedCount` int(11) NOT NULL,
  `FacebookId` bigint(20) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Categories` (
  `CategoryId` int(11) NOT NULL AUTO_INCREMENT,
  `Language` longtext,
  `Value` longtext,
  `CreatedDate` datetime(6) NOT NULL,
  `Source` longtext,
  `Comment` longtext,
  PRIMARY KEY (`CategoryId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `CrosswordTemplates` (
  `CrosswordTemplateId` int(11) NOT NULL AUTO_INCREMENT,
  `Cols` bigint(20) NOT NULL,
  `Rows` bigint(20) NOT NULL,
  `GridCollection` longtext,
  PRIMARY KEY (`CrosswordTemplateId`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `DictionaryUsers` (
  `UserId` int(11) NOT NULL AUTO_INCREMENT,
  `FirstName` longtext,
  `LastName` longtext,
  `UserName` longtext,
  `ExternalId` longtext,
  PRIMARY KEY (`UserId`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Hints` (
  `HintId` int(11) NOT NULL AUTO_INCREMENT,
  `Language` longtext,
  `Value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs,
  `NumberOfLetters` int(11) NOT NULL,
  `NumberOfWords` int(11) NOT NULL,
  `UserId` int(11) DEFAULT NULL,
  `CreatedDate` datetime(6) NOT NULL,
  PRIMARY KEY (`HintId`),
  KEY `IX_Hints_UserId` (`UserId`),
  CONSTRAINT `FK_Hints_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `RefreshTokens` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ApplicationUserId` varchar(255) NOT NULL,
  `Created` datetime(6) NOT NULL,
  `Modified` datetime(6) NOT NULL,
  `Token` longtext,
  `Expires` datetime(6) NOT NULL,
  `RemoteIpAddress` longtext,
  `UserAgent` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_RefreshTokens_ApplicationUserId` (`ApplicationUserId`),
  CONSTRAINT `FK_RefreshTokens_AspNetUsers_ApplicationUserId` FOREIGN KEY (`ApplicationUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=212 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `States` (
  `StateId` int(11) NOT NULL AUTO_INCREMENT,
  `NumberOfLetters` int(11) NOT NULL DEFAULT '0',
  `CreatedDate` datetime(6) NOT NULL,
  `Source` longtext,
  `Comment` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs DEFAULT NULL,
  `Word` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs DEFAULT NULL,
  PRIMARY KEY (`StateId`)
) ENGINE=InnoDB AUTO_INCREMENT=297 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Users` (
  `UserId` int(11) NOT NULL AUTO_INCREMENT,
  `FirstName` longtext,
  `LastName` longtext,
  `UserName` longtext,
  `Password` longtext,
  `isVIP` smallint(6) NOT NULL,
  `ExternalId` longtext,
  PRIMARY KEY (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `WordHint` (
  `WordId` int(11) NOT NULL,
  `HintId` int(11) NOT NULL,
  PRIMARY KEY (`WordId`,`HintId`),
  KEY `IX_WordHint_HintId` (`HintId`),
  CONSTRAINT `FK_WordHint_Hints_HintId` FOREIGN KEY (`HintId`) REFERENCES `Hints` (`HintId`) ON DELETE CASCADE,
  CONSTRAINT `FK_WordHint_Words_WordId` FOREIGN KEY (`WordId`) REFERENCES `Words` (`WordId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `WordRelations` (
  `WordFromId` int(11) NOT NULL,
  `WordToId` int(11) NOT NULL,
  `Comment` longtext,
  `CreatedDate` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  `Source` longtext,
  PRIMARY KEY (`WordFromId`,`WordToId`),
  KEY `IX_WordRelations_WordToId` (`WordToId`),
  CONSTRAINT `FK_WordRelations_Words_WordFromId` FOREIGN KEY (`WordFromId`) REFERENCES `Words` (`WordId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_WordRelations_Words_WordToId` FOREIGN KEY (`WordToId`) REFERENCES `Words` (`WordId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Words` (
  `WordId` int(11) NOT NULL AUTO_INCREMENT,
  `Language` longtext,
  `Value` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs DEFAULT NULL,
  `NumberOfLetters` int(11) NOT NULL,
  `NumberOfWords` int(11) NOT NULL,
  `UserId` int(11) DEFAULT NULL,
  `CreatedDate` datetime(6) NOT NULL,
  `Comment` longtext,
  `Source` longtext,
  `CategoryId` int(11) DEFAULT NULL,
  PRIMARY KEY (`WordId`),
  UNIQUE KEY `IX_Words_Value` (`Value`),
  KEY `IX_Words_UserId` (`UserId`),
  KEY `IX_Words_CategoryId` (`CategoryId`),
  CONSTRAINT `FK_Words_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`CategoryId`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Words_DictionaryUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `DictionaryUsers` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=784447 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

# this section was created using:
# mysqldump -P 3360 --protocol=tcp -uroot -psecret --compact dictionary __EFMigrationsHistory > export_efmigratonhistory.sql

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(95) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
INSERT INTO `__EFMigrationsHistory` VALUES ('20190310133646_InitialCreate','2.2.4-servicing-10062'),('20190401032038_AddedIdentity','2.2.4-servicing-10062'),('20190403004557_RenamedUserList','2.2.4-servicing-10062'),('20190414145821_SelfReference','2.2.4-servicing-10062'),('20190415231402_UserCleanup','2.2.4-servicing-10062'),('20190416160657_WordSourceComment','2.2.4-servicing-10062'),('20190423165133_State','2.2.4-servicing-10062'),('20190424190554_StateChanges','2.2.4-servicing-10062'),('20190428142018_CrosswordTemplate','2.2.4-servicing-10062'),('20190529170709_StatesCollations','2.2.4-servicing-10062'),('20190628212801_WordRelationDateAndComment','2.2.4-servicing-10062'),('20190723121503_ApplicationUserAndRefreshToken','2.2.6-servicing-10079'),('20191106214138_RefreshTokenWithUserAgent','2.2.6-servicing-10079');
