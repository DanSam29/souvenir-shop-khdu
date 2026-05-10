CREATE DATABASE  IF NOT EXISTS `khdu_souvenir_shop` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `khdu_souvenir_shop`;
-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: localhost    Database: khdu_souvenir_shop
-- ------------------------------------------------------
-- Server version	9.5.0

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
SET @MYSQLDUMP_TEMP_LOG_BIN = @@SESSION.SQL_LOG_BIN;
SET @@SESSION.SQL_LOG_BIN= 0;

--
-- GTID state at the beginning of the backup 
--

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '96ab3e1e-d8b5-11f0-87fb-0c7955d6bd01:1-701,
9caec02e-d83f-11f0-84d6-d45d64b0c160:1-87';

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20260429074113_InitialBaseline','8.0.0'),('20260502140728_AddCreatedByUserIdColumns','8.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cart`
--

DROP TABLE IF EXISTS `cart`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cart` (
  `CartId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`CartId`),
  UNIQUE KEY `UserId` (`UserId`),
  CONSTRAINT `cart_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cart`
--

LOCK TABLES `cart` WRITE;
/*!40000 ALTER TABLE `cart` DISABLE KEYS */;
INSERT INTO `cart` VALUES (1,1,'2026-02-11 01:51:18'),(2,3,'2026-02-11 01:51:56'),(3,4,'2026-02-11 04:45:01'),(5,6,'2026-02-11 06:45:25'),(8,12,'2026-05-05 10:55:55'),(9,5,'2026-05-06 14:41:39');
/*!40000 ALTER TABLE `cart` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cartitems`
--

DROP TABLE IF EXISTS `cartitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cartitems` (
  `CartItemId` int NOT NULL AUTO_INCREMENT,
  `CartId` int NOT NULL,
  `ProductId` int NOT NULL,
  `Quantity` int NOT NULL DEFAULT '1',
  `AddedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`CartItemId`),
  UNIQUE KEY `unique_cart_product` (`CartId`,`ProductId`),
  KEY `cartitems_ibfk_2` (`ProductId`),
  CONSTRAINT `cartitems_ibfk_1` FOREIGN KEY (`CartId`) REFERENCES `cart` (`CartId`) ON DELETE CASCADE,
  CONSTRAINT `cartitems_ibfk_2` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=32 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cartitems`
--

LOCK TABLES `cartitems` WRITE;
/*!40000 ALTER TABLE `cartitems` DISABLE KEYS */;
INSERT INTO `cartitems` VALUES (3,3,1,1,'2026-02-11 04:45:18'),(5,5,1,1,'2026-02-11 06:45:41');
/*!40000 ALTER TABLE `cartitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `categories`
--

DROP TABLE IF EXISTS `categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `categories` (
  `CategoryId` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL,
  `NameEn` varchar(100) DEFAULT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `DescriptionEn` varchar(500) DEFAULT NULL,
  `ParentCategoryId` int DEFAULT NULL,
  `DisplayOrder` int NOT NULL DEFAULT '0',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`CategoryId`),
  KEY `categories_ibfk_1` (`ParentCategoryId`),
  CONSTRAINT `categories_ibfk_1` FOREIGN KEY (`ParentCategoryId`) REFERENCES `categories` (`CategoryId`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `categories`
--

LOCK TABLES `categories` WRITE;
/*!40000 ALTER TABLE `categories` DISABLE KEYS */;
INSERT INTO `categories` VALUES (1,'Одяг','Clothing','Брендований одяг ХДУ',NULL,NULL,1,'2026-02-11 03:50:12','2026-05-06 17:25:04'),(2,'Аксесуари','Accessories','Аксесуари та сувеніри',NULL,NULL,2,'2026-02-11 03:50:12','2026-05-06 17:25:04'),(3,'Канцелярія','Stationery','Канцелярські товари з символікою',NULL,NULL,3,'2026-02-11 03:50:12','2026-05-06 17:25:04'),(4,'Футболки','T-Shirts','Футболки з логотипом ХДУ',NULL,1,1,'2026-02-11 03:50:12','2026-05-06 17:25:04'),(5,'Худі','Hoodies','Толстовки та худі',NULL,1,2,'2026-02-11 03:50:12','2026-05-06 17:25:04'),(6,'Кружки','Mugs','Кружки з символікою',NULL,2,1,'2026-02-11 03:50:12','2026-05-06 17:25:04');
/*!40000 ALTER TABLE `categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `companies`
--

DROP TABLE IF EXISTS `companies`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `companies` (
  `CompanyId` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) NOT NULL,
  `ContactPerson` varchar(100) DEFAULT NULL,
  `Phone` varchar(20) DEFAULT NULL,
  `Email` varchar(100) DEFAULT NULL,
  `Address` varchar(500) DEFAULT NULL,
  `Notes` text,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`CompanyId`),
  UNIQUE KEY `Name` (`Name`),
  UNIQUE KEY `Email` (`Email`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `companies`
--

LOCK TABLES `companies` WRITE;
/*!40000 ALTER TABLE `companies` DISABLE KEYS */;
INSERT INTO `companies` VALUES (1,'ТОВ \"Текстиль Плюс\"','Іванов Іван Іванович','+380501234567','textile@example.com','м. Київ, вул. Хрещатик, 1',NULL,1,'2026-02-11 03:50:12','2026-02-11 03:50:12');
/*!40000 ALTER TABLE `companies` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `incomingdocuments`
--

DROP TABLE IF EXISTS `incomingdocuments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `incomingdocuments` (
  `DocumentId` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `Quantity` int NOT NULL,
  `PurchasePrice` decimal(10,2) NOT NULL,
  `CompanyId` int NOT NULL,
  `DocumentDate` date NOT NULL,
  `CreatedByUserId` int DEFAULT NULL,
  `CreatedBy` int NOT NULL,
  `Notes` text,
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`DocumentId`),
  KEY `incomingdocuments_ibfk_1` (`ProductId`),
  KEY `incomingdocuments_ibfk_2` (`CompanyId`),
  KEY `incomingdocuments_ibfk_3` (`CreatedBy`),
  CONSTRAINT `incomingdocuments_ibfk_1` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT,
  CONSTRAINT `incomingdocuments_ibfk_2` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`CompanyId`) ON DELETE RESTRICT,
  CONSTRAINT `incomingdocuments_ibfk_3` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `incomingdocuments`
--

LOCK TABLES `incomingdocuments` WRITE;
/*!40000 ALTER TABLE `incomingdocuments` DISABLE KEYS */;
/*!40000 ALTER TABLE `incomingdocuments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orderhistory`
--

DROP TABLE IF EXISTS `orderhistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orderhistory` (
  `HistoryId` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `OldStatus` varchar(50) DEFAULT NULL,
  `NewStatus` varchar(50) NOT NULL,
  `ChangedBy` int DEFAULT NULL,
  `Comment` text,
  `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`HistoryId`),
  KEY `orderhistory_ibfk_1` (`OrderId`),
  KEY `orderhistory_ibfk_2` (`ChangedBy`),
  CONSTRAINT `orderhistory_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE,
  CONSTRAINT `orderhistory_ibfk_2` FOREIGN KEY (`ChangedBy`) REFERENCES `users` (`UserId`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=20 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orderhistory`
--

LOCK TABLES `orderhistory` WRITE;
/*!40000 ALTER TABLE `orderhistory` DISABLE KEYS */;
INSERT INTO `orderhistory` VALUES (1,31,'Processing','Delivered',1,'Статус змінено адміністратором. ','2026-05-04 06:52:50'),(2,32,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:12:55'),(3,30,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:22:47'),(4,33,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:25:15'),(5,19,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:10'),(6,29,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:26'),(7,28,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:29'),(8,27,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:35'),(9,26,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:40'),(10,24,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:42'),(11,25,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:45'),(12,20,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:49'),(13,21,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:52'),(14,22,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:56'),(15,23,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:34:59'),(16,34,'Processing','Cancelled',1,'Статус змінено адміністратором. ','2026-05-04 08:36:41'),(17,34,'Processing','Cancelled',NULL,'Скасовано адміністратором','2026-05-04 08:43:27'),(18,35,'Processing','Cancelled',NULL,'Скасовано адміністратором','2026-05-05 13:29:24'),(19,36,'Processing','Cancelled',NULL,'Скасовано адміністратором','2026-05-10 06:53:57');
/*!40000 ALTER TABLE `orderhistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orderitems`
--

DROP TABLE IF EXISTS `orderitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orderitems` (
  `OrderItemId` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `ProductId` int NOT NULL,
  `Quantity` int NOT NULL,
  `OriginalPrice` decimal(10,2) NOT NULL,
  `AppliedPromotionId` int DEFAULT NULL,
  `DiscountAmount` decimal(10,2) NOT NULL DEFAULT '0.00',
  `FinalPrice` decimal(10,2) NOT NULL,
  PRIMARY KEY (`OrderItemId`),
  KEY `orderitems_ibfk_1` (`OrderId`),
  KEY `orderitems_ibfk_2` (`ProductId`),
  CONSTRAINT `orderitems_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE,
  CONSTRAINT `orderitems_ibfk_2` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=46 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orderitems`
--

LOCK TABLES `orderitems` WRITE;
/*!40000 ALTER TABLE `orderitems` DISABLE KEYS */;
INSERT INTO `orderitems` VALUES (27,19,1,1,450.00,NULL,0.00,450.00),(28,20,1,1,450.00,NULL,0.00,450.00),(29,21,4,1,800.00,NULL,0.00,800.00),(30,22,1,1,450.00,NULL,0.00,450.00),(31,23,2,1,450.00,NULL,0.00,450.00),(32,24,2,1,450.00,NULL,0.00,450.00),(33,25,1,1,450.00,NULL,22.50,427.50),(34,26,1,1,450.00,NULL,22.50,427.50),(35,26,2,1,450.00,NULL,22.50,427.50),(36,27,1,1,450.00,NULL,22.50,427.50),(37,28,1,1,450.00,NULL,22.50,427.50),(38,29,5,1,200.00,NULL,10.00,190.00),(39,30,7,1,95.00,NULL,4.75,90.25),(40,31,6,1,450.00,NULL,22.50,427.50),(41,32,4,1,800.00,NULL,40.00,760.00),(42,33,1,1,450.00,NULL,22.50,427.50),(43,34,1,1,450.00,NULL,22.50,427.50),(44,35,4,1,800.00,NULL,80.00,720.00),(45,36,5,1,200.00,NULL,10.00,190.00);
/*!40000 ALTER TABLE `orderitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orders`
--

DROP TABLE IF EXISTS `orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orders` (
  `OrderId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `OrderNumber` varchar(50) NOT NULL,
  `Status` enum('Processing','Shipped','Delivered','Cancelled') NOT NULL DEFAULT 'Processing',
  `TotalAmount` decimal(10,2) NOT NULL,
  `ShippingCost` decimal(10,2) NOT NULL DEFAULT '0.00',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`OrderId`),
  UNIQUE KEY `OrderNumber` (`OrderNumber`),
  KEY `orders_ibfk_1` (`UserId`),
  CONSTRAINT `orders_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=37 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orders`
--

LOCK TABLES `orders` WRITE;
/*!40000 ALTER TABLE `orders` DISABLE KEYS */;
INSERT INTO `orders` VALUES (19,3,'ORD-20260502181811-db78f5','Cancelled',522.00,72.00,'2026-05-02 15:18:12','2026-05-04 11:34:10'),(20,3,'ORD-20260503125552-bad644','Cancelled',522.00,72.00,'2026-05-03 09:55:53','2026-05-04 11:34:48'),(21,3,'ORD-20260503133230-e2f341','Cancelled',877.00,77.00,'2026-05-03 10:32:30','2026-05-04 11:34:52'),(22,3,'ORD-20260503134422-0f30cd','Cancelled',522.00,72.00,'2026-05-03 10:44:23','2026-05-04 11:34:56'),(23,3,'ORD-20260503135100-9817c9','Cancelled',522.20,72.20,'2026-05-03 10:51:01','2026-05-04 11:34:58'),(24,3,'ORD-20260503165408-fe86f9','Cancelled',522.20,72.20,'2026-05-03 13:54:09','2026-05-04 11:34:42'),(25,3,'ORD-20260503180808-5e1736','Cancelled',499.50,72.00,'2026-05-03 15:08:08','2026-05-04 11:34:44'),(26,3,'ORD-20260503183239-03bfd1','Cancelled',929.20,74.20,'2026-05-03 15:32:39','2026-05-04 11:34:39'),(27,3,'ORD-20260504065435-b23d55','Cancelled',499.50,72.00,'2026-05-04 03:54:36','2026-05-04 11:34:35'),(28,3,'ORD-20260504071407-6f5988','Cancelled',499.50,72.00,'2026-05-04 04:14:07','2026-05-04 11:34:29'),(29,3,'ORD-20260504072728-69ac76','Cancelled',263.50,73.50,'2026-05-04 04:27:29','2026-05-04 11:34:25'),(30,3,'ORD-20260504082145-446014','Cancelled',162.75,72.50,'2026-05-04 05:21:46','2026-05-04 11:22:47'),(31,3,'ORD-20260504083549-fc2fe3','Delivered',500.30,72.80,'2026-05-04 05:35:50','2026-05-04 09:52:50'),(32,3,'ORD-20260504110712-08aee8','Cancelled',837.00,77.00,'2026-05-04 08:07:13','2026-05-04 11:12:54'),(33,3,'ORD-20260504112355-28aeb4','Cancelled',499.50,72.00,'2026-05-04 08:23:55','2026-05-04 11:25:14'),(34,3,'ORD-20260504113557-3b2c9a','Cancelled',499.50,72.00,'2026-05-04 08:35:58','2026-05-04 11:43:27'),(35,12,'ORD-20260505162242-f809d8','Cancelled',797.00,77.00,'2026-05-05 13:22:43','2026-05-05 16:29:23'),(36,3,'ORD-20260510095247-aa8c56','Cancelled',263.50,73.50,'2026-05-10 06:52:47','2026-05-10 09:53:57');
/*!40000 ALTER TABLE `orders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `outgoingdocuments`
--

DROP TABLE IF EXISTS `outgoingdocuments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `outgoingdocuments` (
  `DocumentId` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `Quantity` int NOT NULL,
  `OrderId` int DEFAULT NULL,
  `CompanyId` int DEFAULT NULL,
  `Reason` enum('Order','Damaged','Lost','Return','Inventory') NOT NULL,
  `OriginalPrice` decimal(10,2) DEFAULT NULL,
  `AppliedPromotionId` int DEFAULT NULL,
  `DiscountAmount` decimal(10,2) DEFAULT NULL,
  `FinalPrice` decimal(10,2) DEFAULT NULL,
  `DocumentDate` date NOT NULL,
  `CreatedByUserId` int DEFAULT NULL,
  `CreatedBy` int DEFAULT NULL,
  `Notes` text,
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`DocumentId`),
  KEY `outgoingdocuments_ibfk_1` (`ProductId`),
  KEY `outgoingdocuments_ibfk_2` (`OrderId`),
  KEY `outgoingdocuments_ibfk_3` (`CompanyId`),
  KEY `outgoingdocuments_ibfk_5` (`CreatedBy`),
  CONSTRAINT `outgoingdocuments_ibfk_1` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT,
  CONSTRAINT `outgoingdocuments_ibfk_2` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE SET NULL,
  CONSTRAINT `outgoingdocuments_ibfk_3` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`CompanyId`) ON DELETE RESTRICT,
  CONSTRAINT `outgoingdocuments_ibfk_5` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserId`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=36 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `outgoingdocuments`
--

LOCK TABLES `outgoingdocuments` WRITE;
/*!40000 ALTER TABLE `outgoingdocuments` DISABLE KEYS */;
INSERT INTO `outgoingdocuments` VALUES (17,1,1,19,NULL,'Order',450.00,NULL,0.00,450.00,'2026-05-02',3,NULL,'Автоматично створено для замовлення ORD-20260502181811-db78f5','2026-05-02 15:18:12'),(18,1,1,20,NULL,'Order',450.00,NULL,0.00,450.00,'2026-05-03',3,NULL,'Автоматично створено для замовлення ORD-20260503125552-bad644','2026-05-03 09:55:53'),(19,4,1,21,NULL,'Order',800.00,NULL,0.00,800.00,'2026-05-03',3,NULL,'Автоматично створено для замовлення ORD-20260503133230-e2f341','2026-05-03 10:32:30'),(20,1,1,22,NULL,'Order',450.00,NULL,0.00,450.00,'2026-05-03',3,NULL,'Автоматично створено для замовлення ORD-20260503134422-0f30cd','2026-05-03 10:44:23'),(21,2,1,23,NULL,'Order',450.00,NULL,0.00,450.00,'2026-05-03',3,NULL,'Автоматично створено для замовлення ORD-20260503135100-9817c9','2026-05-03 10:51:01'),(22,2,1,24,NULL,'Order',450.00,NULL,0.00,450.00,'2026-05-03',3,NULL,'Автоматично створено для замовлення ORD-20260503165408-fe86f9','2026-05-03 13:54:09'),(23,1,1,25,NULL,'Order',450.00,NULL,22.50,427.50,'2026-05-03',3,NULL,'Автоматично створено для замовлення ORD-20260503180808-5e1736','2026-05-03 15:08:08'),(24,1,1,26,NULL,'Order',450.00,NULL,22.50,427.50,'2026-05-03',3,NULL,'Автоматично створено для замовлення ORD-20260503183239-03bfd1','2026-05-03 15:32:39'),(25,2,1,26,NULL,'Order',450.00,NULL,22.50,427.50,'2026-05-03',3,NULL,'Автоматично створено для замовлення ORD-20260503183239-03bfd1','2026-05-03 15:32:39'),(26,1,1,27,NULL,'Order',450.00,NULL,22.50,427.50,'2026-05-04',3,NULL,'Автоматично створено для замовлення ORD-20260504065435-b23d55','2026-05-04 03:54:36'),(27,1,1,28,NULL,'Order',450.00,NULL,22.50,427.50,'2026-05-04',3,NULL,'Автоматично створено для замовлення ORD-20260504071407-6f5988','2026-05-04 04:14:07'),(28,5,1,29,NULL,'Order',200.00,NULL,10.00,190.00,'2026-05-04',3,NULL,'Автоматично створено для замовлення ORD-20260504072728-69ac76','2026-05-04 04:27:29'),(29,7,1,30,NULL,'Order',95.00,NULL,4.75,90.25,'2026-05-04',3,NULL,'Автоматично створено для замовлення ORD-20260504082145-446014','2026-05-04 05:21:46'),(30,6,1,31,NULL,'Order',450.00,NULL,22.50,427.50,'2026-05-04',3,NULL,'Автоматично створено для замовлення ORD-20260504083549-fc2fe3','2026-05-04 05:35:50'),(31,4,1,32,NULL,'Order',800.00,NULL,40.00,760.00,'2026-05-04',NULL,3,'Автоматично створено для замовлення ORD-20260504110712-08aee8','2026-05-04 08:07:13'),(32,1,1,33,NULL,'Order',450.00,NULL,22.50,427.50,'2026-05-04',NULL,3,'Автоматично створено для замовлення ORD-20260504112355-28aeb4','2026-05-04 08:23:55');
/*!40000 ALTER TABLE `outgoingdocuments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `payments`
--

DROP TABLE IF EXISTS `payments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `payments` (
  `PaymentId` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `Method` enum('Card','CashOnDelivery') NOT NULL,
  `Status` enum('Pending','Completed','Failed','Refunded') NOT NULL DEFAULT 'Pending',
  `TransactionId` varchar(255) DEFAULT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `StripeSessionId` varchar(255) DEFAULT NULL,
  `StripePaymentIntentId` varchar(255) DEFAULT NULL,
  `IdempotencyKey` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`PaymentId`),
  UNIQUE KEY `OrderId` (`OrderId`),
  CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=22 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `payments`
--

LOCK TABLES `payments` WRITE;
/*!40000 ALTER TABLE `payments` DISABLE KEYS */;
INSERT INTO `payments` VALUES (4,19,522.00,'Card','Pending',NULL,'2026-05-02 15:18:12','2026-05-02 18:18:17','cs_test_a1wEUHdWoKLbAxafSTN90EqYFzknnQXy48dIIAW8Wk7A4JzWueyoOLQPwI',NULL,'423a54b0-6274-4464-ad2c-ac9a857364ec'),(5,20,522.00,'Card','Pending',NULL,'2026-05-03 09:55:53','2026-05-03 12:55:56','cs_test_a1GFJLx4Aj2t6dk1hVXnziBUktIzRcVlSAuK6PCSPpN0UpLabY6xvzVL6T',NULL,'4ea141e6-8af5-417d-bfdb-25a4cff55bfa'),(6,21,877.00,'Card','Pending',NULL,'2026-05-03 10:32:30','2026-05-03 13:32:35','cs_test_a1UcXF2SlMwG2qm9yZc4wO0JcIpkrbegysPxEAJlEtRW9ovXLpHmvTq9pl',NULL,'0f4d2c6b-ec08-4fba-802c-8f852c97065f'),(7,22,522.00,'Card','Pending',NULL,'2026-05-03 10:44:23','2026-05-03 13:44:26','cs_test_a1jcfVZjwcai8nslNIW3AymCwbjNd5qErDS53nVpXlnzazsneySEIUXLOu',NULL,'9fa6ba17-7aad-4d89-af1a-5d3f3926427c'),(8,23,522.20,'Card','Pending',NULL,'2026-05-03 10:51:01','2026-05-03 13:51:05','cs_test_a1TqjgQxwkz4B7VlzCLNtTNk5vGYfwZZe9BiNyJmNvJZUlHn0C4VoK2z5K',NULL,'fb74fd80-e009-4e0f-8e71-a8714873447b'),(9,24,522.20,'Card','Pending',NULL,'2026-05-03 13:54:09','2026-05-03 16:54:14','cs_test_b1cn6QVgiNn7znPWIyWzEVsSpI413hdrnRC7xV47blwEcAVFugq8nqhXun',NULL,'25480889-9f83-4d2c-8b27-5076f72809ac'),(10,25,499.50,'Card','Pending',NULL,'2026-05-03 15:08:08','2026-05-03 18:08:13','cs_test_b1yMbNqQugZdNZeay2ZEgQX6DElIx6CZ0SmAe2pDvwGro3MdJASAT0WsTG',NULL,'bcce2d22-19af-4464-a432-22430172dd11'),(11,26,929.20,'Card','Pending',NULL,'2026-05-03 15:32:39','2026-05-03 18:32:43','cs_test_b1VeNAj2R5qd8rKheeNW8LvB9wbFCvPSKWH5j0SNNeq5WRjojXL0goYLa0',NULL,'8beef0ee-7aad-4325-bc36-41de9ecb4225'),(12,27,499.50,'CashOnDelivery','Pending',NULL,'2026-05-04 03:54:36',NULL,NULL,NULL,NULL),(13,28,499.50,'CashOnDelivery','Pending',NULL,'2026-05-04 04:14:07',NULL,NULL,NULL,NULL),(14,29,263.50,'CashOnDelivery','Pending',NULL,'2026-05-04 04:27:29',NULL,NULL,NULL,NULL),(15,30,162.75,'CashOnDelivery','Pending',NULL,'2026-05-04 05:21:46',NULL,NULL,NULL,NULL),(16,31,500.30,'CashOnDelivery','Completed',NULL,'2026-05-04 05:35:50','2026-05-04 09:52:50',NULL,NULL,NULL),(17,32,837.00,'CashOnDelivery','Pending',NULL,'2026-05-04 08:07:13',NULL,NULL,NULL,NULL),(18,33,499.50,'CashOnDelivery','Pending',NULL,'2026-05-04 08:23:55',NULL,NULL,NULL,NULL),(19,34,499.50,'CashOnDelivery','Failed',NULL,'2026-05-04 08:35:58','2026-05-04 11:43:27',NULL,NULL,NULL),(20,35,797.00,'CashOnDelivery','Failed',NULL,'2026-05-05 13:22:43','2026-05-05 16:29:23',NULL,NULL,NULL),(21,36,263.50,'Card','Failed',NULL,'2026-05-10 06:52:47','2026-05-10 09:53:57','cs_test_b1IaSXfLIIKylfsLk2AeljvXHhvgDKWEqIJwvJXdwTL8hQTGbS6RABEE7b',NULL,'e85a1b81-9a05-416e-8e14-6eb2ff5222b5');
/*!40000 ALTER TABLE `payments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `productimages`
--

DROP TABLE IF EXISTS `productimages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productimages` (
  `ImageId` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `ImageURL` varchar(500) NOT NULL,
  `IsPrimary` tinyint(1) NOT NULL DEFAULT '0',
  `DisplayOrder` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`ImageId`),
  KEY `productimages_ibfk_1` (`ProductId`),
  CONSTRAINT `productimages_ibfk_1` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `productimages`
--

LOCK TABLES `productimages` WRITE;
/*!40000 ALTER TABLE `productimages` DISABLE KEYS */;
INSERT INTO `productimages` VALUES (1,1,'/images/products/tshirt-white-front.jpg',1,1),(2,1,'/images/products/tshirt-white-back.jpg',0,2),(3,2,'/images/products/tshirt-blue-front.jpg',1,1),(4,2,'/images/products/tshirt-blue-back.jpg',0,2),(5,3,'/images/products/hoodie-black-front.jpg',1,1),(6,3,'/images/products/hoodie-black-back.jpg',0,2),(7,4,'/images/products/hoodie-grey-front.jpg',1,1),(8,4,'/images/products/hoodie-grey-back.jpg',0,2),(9,5,'/images/products/mug-ceramic-white.jpg',1,1),(10,6,'/images/products/mug-thermo-steel.jpg',1,1),(11,7,'/images/products/notebook-a5.jpg',1,1),(12,8,'/images/products/pen-metal.jpg',1,1);
/*!40000 ALTER TABLE `productimages` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `products`
--

DROP TABLE IF EXISTS `products`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `products` (
  `ProductId` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) NOT NULL,
  `NameEn` varchar(200) DEFAULT NULL,
  `Description` text NOT NULL,
  `DescriptionEn` text,
  `Price` decimal(10,2) NOT NULL,
  `Weight` decimal(10,3) NOT NULL,
  `CategoryId` int NOT NULL,
  `Stock` int NOT NULL DEFAULT '0',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`ProductId`),
  KEY `products_ibfk_1` (`CategoryId`),
  CONSTRAINT `products_ibfk_1` FOREIGN KEY (`CategoryId`) REFERENCES `categories` (`CategoryId`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `products`
--

LOCK TABLES `products` WRITE;
/*!40000 ALTER TABLE `products` DISABLE KEYS */;
INSERT INTO `products` VALUES (1,'Футболка ХДУ біла','White T-Shirt','Класична біла футболка','Classic white t-shirt with KSU logo',450.00,0.200,4,7,'2026-02-11 03:59:34','2026-05-06 17:25:04'),(2,'Футболка ХДУ синя','Blue T-Shirt','Синя футболка з гербом','Blue t-shirt with KSU emblem',450.00,0.220,4,8,'2026-02-11 03:59:34','2026-05-06 17:25:04'),(3,'Худі ХДУ чорне','Black Hoodie','Тепле чорне худі','Warm black hoodie with KSU emblem',800.00,0.650,5,0,'2026-02-11 03:59:34','2026-05-06 17:25:04'),(4,'Худі ХДУ сіре','Grey Hoodie','Стильне сіре худі','Stylish grey hoodie with KSU emblem',800.00,0.700,5,6,'2026-02-11 03:59:34','2026-05-06 17:25:04'),(5,'Кружка ХДУ','KSU Mug','Керамічна кружка 350мл','Ceramic mug 350ml',200.00,0.350,6,21,'2026-02-11 03:59:34','2026-05-10 09:53:57'),(6,'Термокружка ХДУ','Thermo Mug','Сталева термокружка 450мл','Steel thermo mug 450ml',450.00,0.280,6,11,'2026-02-11 03:59:34','2026-05-06 17:25:04'),(7,'Блокнот ХДУ','KSU Notebook','Блокнот А5 в клітинку','A5 checkered notebook',95.00,0.250,3,2,'2026-02-11 03:59:34','2026-05-06 17:25:04'),(8,'Ручка ХДУ','KSU Pen','Металева ручка в футлярі','Metal pen in a case',30.00,0.080,3,0,'2026-02-11 03:59:34','2026-05-06 17:25:04');
/*!40000 ALTER TABLE `products` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `promotions`
--

DROP TABLE IF EXISTS `promotions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `promotions` (
  `PromotionId` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) NOT NULL,
  `NameEn` varchar(200) DEFAULT NULL,
  `Description` text,
  `DescriptionEn` text,
  `Type` enum('PERCENTAGE','FIXED_AMOUNT','SPECIAL_PRICE') NOT NULL,
  `Value` decimal(10,2) NOT NULL,
  `TargetType` enum('PRODUCT','CATEGORY','CART','SHIPPING') NOT NULL,
  `TargetId` int DEFAULT NULL,
  `AudienceType` enum('ALL','STUDENTS','STAFF','ALUMNI','CUSTOM','NONE','REGULAR','SCHOLARSHIP','HIGH_ACHIEVER') NOT NULL DEFAULT 'ALL',
  `StartDate` timestamp NULL DEFAULT NULL,
  `EndDate` timestamp NULL DEFAULT NULL,
  `PromoCode` varchar(50) DEFAULT NULL,
  `MinOrderAmount` decimal(10,2) DEFAULT NULL,
  `MinQuantity` int DEFAULT NULL,
  `Priority` int NOT NULL DEFAULT '0',
  `UsageLimit` int DEFAULT NULL,
  `CurrentUsage` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedBy` int NOT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`PromotionId`),
  UNIQUE KEY `PromoCode` (`PromoCode`),
  KEY `promotions_ibfk_1` (`CreatedBy`),
  CONSTRAINT `promotions_ibfk_1` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `promotions`
--

LOCK TABLES `promotions` WRITE;
/*!40000 ALTER TABLE `promotions` DISABLE KEYS */;
INSERT INTO `promotions` VALUES (2,'Знижка REGULAR','REGULAR Discount','5% для REGULAR','5% for REGULAR status','PERCENTAGE',5.00,'CART',NULL,'REGULAR',NULL,NULL,NULL,NULL,NULL,10,NULL,0,1,2,'2026-02-11 08:07:08','2026-05-06 18:00:11'),(3,'Знижка SCHOLARSHIP','SCHOLARSHIP Discount','10% для SCHOLARSHIP','10% for SCHOLARSHIP status','PERCENTAGE',10.00,'CART',NULL,'SCHOLARSHIP',NULL,NULL,NULL,NULL,NULL,20,NULL,0,1,2,'2026-02-11 08:07:08','2026-05-06 18:00:11'),(4,'Знижка HIGH_ACHIEVER','HIGH_ACHIEVER Discount','15% для HIGH_ACHIEVER','15% for HIGH_ACHIEVER status','PERCENTAGE',15.00,'CART',NULL,'HIGH_ACHIEVER',NULL,NULL,NULL,NULL,NULL,30,NULL,0,1,2,'2026-02-11 08:07:08','2026-05-06 18:00:11'),(5,'Промокод KSU2026','Promo code KSU2026','5% за промокодом','5% discount by promo code','PERCENTAGE',5.00,'CART',NULL,'ALL',NULL,NULL,'KSU2026',NULL,NULL,5,NULL,0,1,2,'2026-02-11 08:07:08','2026-05-06 18:00:11');
/*!40000 ALTER TABLE `promotions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `shipping`
--

DROP TABLE IF EXISTS `shipping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `shipping` (
  `ShippingId` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `City` varchar(100) NOT NULL,
  `CityRef` varchar(100) DEFAULT NULL,
  `WarehouseNumber` varchar(500) NOT NULL,
  `WarehouseRef` varchar(100) DEFAULT NULL,
  `RecipientName` varchar(100) NOT NULL,
  `RecipientPhone` varchar(20) NOT NULL,
  `TrackingNumber` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`ShippingId`),
  UNIQUE KEY `OrderId` (`OrderId`),
  CONSTRAINT `shipping_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `shipping`
--

LOCK TABLES `shipping` WRITE;
/*!40000 ALTER TABLE `shipping` DISABLE KEYS */;
INSERT INTO `shipping` VALUES (1,19,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(2,20,'Миколаїв (Львівська обл., Радехівський р-н)','1b7d7822-8859-11e9-898c-005056b24375','Пункт приймання-видачі (до 30 кг): вул. Зарічна, 1а','19de2a8e-93f3-11e9-898c-005056b24375','Олександр Петренко','+380503333333',NULL),(3,21,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(4,22,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(5,23,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(6,24,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(7,25,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(8,26,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(9,27,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(10,28,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(11,29,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(12,30,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(13,31,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(14,32,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(15,33,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(16,34,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL),(17,35,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Данило Самородський','+380959193802',NULL),(18,36,'Миколаїв','db5c888c-391c-11dd-90d9-001a92567626','Відділення №32 (до 30 кг на одне місце): вул. Ігоря Бедзая, 108/9','3dad4671-5e60-11eb-a5a6-b8830365bd14','Олександр Петренко','+380503333333',NULL);
/*!40000 ALTER TABLE `shipping` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `userpromotions`
--

DROP TABLE IF EXISTS `userpromotions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `userpromotions` (
  `UserPromotionId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `PromotionId` int NOT NULL,
  `AssignedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UsedCount` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`UserPromotionId`),
  UNIQUE KEY `unique_user_promotion` (`UserId`,`PromotionId`),
  KEY `userpromotions_ibfk_2` (`PromotionId`),
  CONSTRAINT `userpromotions_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE,
  CONSTRAINT `userpromotions_ibfk_2` FOREIGN KEY (`PromotionId`) REFERENCES `promotions` (`PromotionId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `userpromotions`
--

LOCK TABLES `userpromotions` WRITE;
/*!40000 ALTER TABLE `userpromotions` DISABLE KEYS */;
/*!40000 ALTER TABLE `userpromotions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `UserId` int NOT NULL AUTO_INCREMENT,
  `FirstName` varchar(50) NOT NULL,
  `LastName` varchar(50) NOT NULL,
  `Email` varchar(100) NOT NULL,
  `Password` varchar(255) NOT NULL,
  `Phone` varchar(20) DEFAULT NULL,
  `Role` enum('Guest','Customer','Manager','Administrator','SuperAdmin') NOT NULL DEFAULT 'Customer',
  `StudentStatus` enum('NONE','REGULAR','SCHOLARSHIP','HIGH_ACHIEVER') NOT NULL DEFAULT 'NONE',
  `GPA` decimal(3,2) DEFAULT NULL,
  `StudentVerifiedAt` timestamp NULL DEFAULT NULL,
  `StudentExpiresAt` timestamp NULL DEFAULT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `Email` (`Email`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'Адміністратор','Системи','admin@university.ks.ua','$2a$12$q6rMaO/aLNvEvjFPEnqXmOf.qdyw6uOnDMEbuRYb3yXy1DvIUf6Ne','+380501111111','Administrator','NONE',NULL,NULL,NULL,'2026-02-11 03:50:12'),(2,'Менеджер','Магазину','manager@university.ks.ua','$2a$12$1FExVgtZVezAVD.vH7gin.N9Oz2cdfwZPDLViCgWJ4XY0t1D2c/Wq','+380952222222','Manager','NONE',NULL,NULL,NULL,'2026-02-11 03:50:12'),(3,'Олександр','Петренко','petrenko@university.ks.ua','$2a$12$e9iMBQZS7ON.Xbtt/Y8Fuu4YtRD3HEYwkD75eZlRr.1FVl/MQAQI2','+380503333333','Customer','REGULAR',3.50,'2026-02-11 03:50:12','2026-06-11 01:50:12','2026-02-11 03:50:12'),(4,'Марія','Коваленко','kovalenko@university.ks.ua','$2a$12$e9iMBQZS7ON.Xbtt/Y8Fuu4YtRD3HEYwkD75eZlRr.1FVl/MQAQI2','+380954444444','Customer','SCHOLARSHIP',4.20,'2026-02-11 03:50:12','2026-06-11 01:50:12','2026-02-11 03:50:12'),(5,'Дмитро','Шевченко','shevchenko@university.ks.ua','$2a$12$e9iMBQZS7ON.Xbtt/Y8Fuu4YtRD3HEYwkD75eZlRr.1FVl/MQAQI2','+380505555555','Customer','HIGH_ACHIEVER',4.80,'2026-02-11 03:50:12','2026-06-11 01:50:12','2026-02-11 03:50:12'),(6,'Іван','Мельник','melnyk@gmail.com','$2a$12$e9iMBQZS7ON.Xbtt/Y8Fuu4YtRD3HEYwkD75eZlRr.1FVl/MQAQI2','+380956666666','Guest','NONE',NULL,NULL,NULL,'2026-02-11 03:50:12'),(12,'Данило','Самородський','021912@university.kherson.ua','$2a$12$e9iMBQZS7ON.Xbtt/Y8Fuu4YtRD3HEYwkD75eZlRr.1FVl/MQAQI2','+380959193802','Customer','SCHOLARSHIP',4.50,'2026-05-05 10:55:16','2026-09-05 10:55:16','2026-05-05 10:55:15');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
SET @@SESSION.SQL_LOG_BIN = @MYSQLDUMP_TEMP_LOG_BIN;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-05-10 12:56:23
