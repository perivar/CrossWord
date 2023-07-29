USE dictionary;
-- MySQL dump 10.14  Distrib 5.5.68-MariaDB, for Linux (x86_64)
--
-- Host: localhost    Database: dictionary
-- ------------------------------------------------------
-- Server version	8.0.15

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Dumping data for table `AspNetUserClaims`
--

LOCK TABLES `AspNetUserClaims` WRITE;
/*!40000 ALTER TABLE `AspNetUserClaims` DISABLE KEYS */;
INSERT INTO `AspNetUserClaims` VALUES (1,'ec24a3dd-ec08-4344-a196-b6195f8275da','http://schemas.microsoft.com/ws/2008/06/identity/claims/role','Admin'),(2,'ec24a3dd-ec08-4344-a196-b6195f8275da','http://schemas.microsoft.com/ws/2008/06/identity/claims/role','Mann');
/*!40000 ALTER TABLE `AspNetUserClaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `AspNetUsers`
--

LOCK TABLES `AspNetUsers` WRITE;
/*!40000 ALTER TABLE `AspNetUsers` DISABLE KEYS */;
INSERT INTO `AspNetUsers` VALUES ('a1d9cd3c-02fc-4c05-a110-ae89684aa109','anne','ANNE','anne@nerseth.com','ANNE@NERSETH.COM','\0','AQAAAAEAACcQAAAAEFX7hkbOPHwB918ynv2SEAG7rPqn558Sb7PFF4y5OZzcgXvaCG7EI20pl5PWp9YzuQ==','MMHRVGIBYACLNOSPVPNV3K7CISAPP6P5','d437d84b-92e5-46ff-9fb8-f18d6b120deb','+47 41 31 88 53','\0','\0',NULL,'',0,NULL),('aca764b2-daa5-4e83-b324-d917ddc4e649','server@wazalo.com','SERVER@WAZALO.COM','server@wazalo.com','SERVER@WAZALO.COM','\0','AQAAAAEAACcQAAAAEBdMXyAGbgVMn7CvKKOMoQCDsHgKr8lGhJclT/d5pzct5MNvFqAPvxfnAkFBMa80tg==','LBBO4S4QIMMX3OHEREHPT6WTGCTNSJHZ','32c10993-76c8-4aef-909c-051918ec0a8a','','\0','\0',NULL,'',0,NULL),('d7f0224d-1d2c-4966-b9eb-40abce839658','annepanne@nerseth.com','ANNEPANNE@NERSETH.COM','anne@panne.com','ANNE@PANNE.COM','\0','AQAAAAEAACcQAAAAED4QDq3bR36TYCbwZTe+rvWszJ00qVReQ5y+n1cM6yKQ4ucybVNAPeUbZckLKkkglg==','E75VL7IXNJOYQ4CUEVJOWUQXHTEM7HLU','b58017cf-2d00-41af-bcf2-dae3295866ab','41    31 88 53','\0','\0',NULL,'',0,NULL),('ec24a3dd-ec08-4344-a196-b6195f8275da','perivar@nerseth.com','PERIVAR@NERSETH.COM','perivar@nerseth.com','PERIVAR@NERSETH.COM','\0','AQAAAAEAACcQAAAAEMAAvahPSQ+kFuq/+bYndoOfY+rWs12F7nLCJo1otYzrnegasfayV2OwWDrjz6DJnw==','7JKC3C7UTFOQCJBDP7UY42GJMR3HPFOE','7ccad81e-c236-4697-9294-c818f49a3d7d','90156615','\0','\0',NULL,'',0,NULL);
/*!40000 ALTER TABLE `AspNetUsers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `DictionaryUsers`
--

LOCK TABLES `DictionaryUsers` WRITE;
/*!40000 ALTER TABLE `DictionaryUsers` DISABLE KEYS */;
INSERT INTO `DictionaryUsers` VALUES (1,'','Admin','admin',NULL);
/*!40000 ALTER TABLE `DictionaryUsers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `__EFMigrationsHistory`
--

LOCK TABLES `__EFMigrationsHistory` WRITE;
/*!40000 ALTER TABLE `__EFMigrationsHistory` DISABLE KEYS */;
INSERT INTO `__EFMigrationsHistory` VALUES ('20190310133646_InitialCreate','2.2.4-servicing-10062'),('20190401032038_AddedIdentity','2.2.4-servicing-10062'),('20190403004557_RenamedUserList','2.2.4-servicing-10062'),('20190414145821_SelfReference','2.2.4-servicing-10062'),('20190415231402_UserCleanup','2.2.4-servicing-10062'),('20190416160657_WordSourceComment','2.2.4-servicing-10062'),('20190423165133_State','2.2.4-servicing-10062'),('20190424190554_StateChanges','2.2.4-servicing-10062'),('20190428142018_CrosswordTemplate','2.2.4-servicing-10062'),('20190529170709_StatesCollations','2.2.4-servicing-10062'),('20190628212801_WordRelationDateAndComment','2.2.4-servicing-10062'),('20190723121503_ApplicationUserAndRefreshToken','2.2.6-servicing-10079'),('20191106214138_RefreshTokenWithUserAgent','2.2.6-servicing-10079');
/*!40000 ALTER TABLE `__EFMigrationsHistory` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2023-07-29  2:14:03
