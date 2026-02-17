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

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '96ab3e1e-d8b5-11f0-87fb-0c7955d6bd01:1-376';

--
-- Table structure for table `cart`
--

DROP TABLE IF EXISTS `cart`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cart` (
  `CartId` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL COMMENT 'Один кошик на користувача',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`CartId`),
  UNIQUE KEY `UserId` (`UserId`),
  KEY `idx_user` (`UserId`),
  CONSTRAINT `cart_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cart`
--

LOCK TABLES `cart` WRITE;
/*!40000 ALTER TABLE `cart` DISABLE KEYS */;
INSERT INTO `cart` VALUES (1,1,'2026-02-11 03:51:18'),(2,3,'2026-02-11 03:51:56'),(3,4,'2026-02-11 06:45:01'),(4,7,'2026-02-11 08:35:37'),(5,6,'2026-02-11 08:45:25');
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
  `Quantity` int NOT NULL DEFAULT '1' COMMENT 'Кількість товару в кошику',
  `AddedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`CartItemId`),
  UNIQUE KEY `unique_cart_product` (`CartId`,`ProductId`) COMMENT 'Унікальна пара: один товар раз у кошику',
  KEY `idx_cart` (`CartId`),
  KEY `idx_product` (`ProductId`),
  CONSTRAINT `cartitems_ibfk_1` FOREIGN KEY (`CartId`) REFERENCES `cart` (`CartId`) ON DELETE CASCADE,
  CONSTRAINT `cartitems_ibfk_2` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cartitems`
--

LOCK TABLES `cartitems` WRITE;
/*!40000 ALTER TABLE `cartitems` DISABLE KEYS */;
INSERT INTO `cartitems` VALUES (3,3,1,1,'2026-02-11 06:45:18'),(4,4,1,2,'2026-02-11 08:35:37'),(5,5,1,1,'2026-02-11 08:45:41'),(6,2,1,1,'2026-02-16 16:06:58'),(7,2,2,2,'2026-02-16 16:07:02');
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
  `Description` varchar(500) DEFAULT NULL,
  `ParentCategoryId` int DEFAULT NULL COMMENT 'FK для ієрархії категорій',
  `DisplayOrder` int NOT NULL DEFAULT '0' COMMENT 'Порядок відображення',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`CategoryId`),
  KEY `idx_name` (`Name`),
  KEY `idx_parent` (`ParentCategoryId`),
  CONSTRAINT `categories_ibfk_1` FOREIGN KEY (`ParentCategoryId`) REFERENCES `categories` (`CategoryId`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `categories`
--

LOCK TABLES `categories` WRITE;
/*!40000 ALTER TABLE `categories` DISABLE KEYS */;
INSERT INTO `categories` VALUES (1,'Одяг','Брендований одяг ХДУ',NULL,1,'2026-02-11 05:50:12',NULL),(2,'Аксесуари','Аксесуари та сувеніри',NULL,2,'2026-02-11 05:50:12',NULL),(3,'Канцелярія','Канцелярські товари з символікою',NULL,3,'2026-02-11 05:50:12',NULL),(4,'Футболки','Футболки з логотипом ХДУ',1,1,'2026-02-11 05:50:12',NULL),(5,'Худі','Толстовки та худі',1,2,'2026-02-11 05:50:12',NULL),(6,'Кружки','Кружки з символікою',2,1,'2026-02-11 05:50:12',NULL);
/*!40000 ALTER TABLE `categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `companies`
--

DROP TABLE IF EXISTS `companies`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `companies` (
  `CompanyId` int NOT NULL AUTO_INCREMENT COMMENT 'Первинний ключ фірми-постачальника',
  `Name` varchar(200) NOT NULL COMMENT 'Назва фірми',
  `ContactPerson` varchar(100) DEFAULT NULL COMMENT 'Контактна особа',
  `Phone` varchar(20) DEFAULT NULL COMMENT 'Телефон',
  `Email` varchar(100) DEFAULT NULL COMMENT 'Email',
  `Address` varchar(500) DEFAULT NULL COMMENT 'Адреса',
  `Notes` text COMMENT 'Примітки',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Чи активна фірма',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`CompanyId`),
  UNIQUE KEY `Name` (`Name`),
  UNIQUE KEY `Email` (`Email`),
  KEY `idx_name` (`Name`),
  KEY `idx_email` (`Email`),
  KEY `idx_active` (`IsActive`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `companies`
--

LOCK TABLES `companies` WRITE;
/*!40000 ALTER TABLE `companies` DISABLE KEYS */;
INSERT INTO `companies` VALUES (1,'ТОВ \"Текстиль Плюс\"','Іванов Іван Іванович','+380501234567','textile@example.com','м. Київ, вул. Хрещатик, 1',NULL,1,'2026-02-11 05:50:12','2026-02-11 05:50:12');
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
  `Quantity` int NOT NULL COMMENT 'Кількість надходження на склад',
  `PurchasePrice` decimal(10,2) NOT NULL COMMENT 'Закупівельна ціна за одиницю',
  `CompanyId` int NOT NULL COMMENT 'Фірма-постачальник',
  `DocumentDate` date NOT NULL,
  `CreatedBy` int NOT NULL COMMENT 'Менеджер, який створив накладну',
  `Notes` text,
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`DocumentId`),
  KEY `idx_product` (`ProductId`),
  KEY `idx_company` (`CompanyId`),
  KEY `idx_date` (`DocumentDate`),
  KEY `idx_created_by` (`CreatedBy`),
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
  `ChangedBy` int DEFAULT NULL COMMENT 'Хто змінив (Admin/Manager)',
  `Comment` text COMMENT 'Коментар, наприклад ТТН',
  `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`HistoryId`),
  KEY `idx_order` (`OrderId`),
  KEY `idx_timestamp` (`Timestamp`),
  KEY `ChangedBy` (`ChangedBy`),
  CONSTRAINT `orderhistory_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE,
  CONSTRAINT `orderhistory_ibfk_2` FOREIGN KEY (`ChangedBy`) REFERENCES `users` (`UserId`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orderhistory`
--

LOCK TABLES `orderhistory` WRITE;
/*!40000 ALTER TABLE `orderhistory` DISABLE KEYS */;
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
  `OriginalPrice` decimal(10,2) NOT NULL COMMENT 'Базова ціна товару (без знижок)',
  `AppliedPromotionId` int DEFAULT NULL COMMENT 'ID застосованої знижки (найвигідніша або промокод)',
  `DiscountAmount` decimal(10,2) NOT NULL DEFAULT '0.00' COMMENT 'Сума знижки в грн',
  `FinalPrice` decimal(10,2) NOT NULL COMMENT 'Фінальна ціна після знижки',
  PRIMARY KEY (`OrderItemId`),
  KEY `idx_order` (`OrderId`),
  KEY `idx_product` (`ProductId`),
  KEY `idx_promotion` (`AppliedPromotionId`),
  CONSTRAINT `orderitems_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE,
  CONSTRAINT `orderitems_ibfk_2` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT,
  CONSTRAINT `orderitems_ibfk_3` FOREIGN KEY (`AppliedPromotionId`) REFERENCES `promotions` (`PromotionId`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orderitems`
--

LOCK TABLES `orderitems` WRITE;
/*!40000 ALTER TABLE `orderitems` DISABLE KEYS */;
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
  `OrderNumber` varchar(50) NOT NULL COMMENT 'Унікальний номер замовлення для клієнта',
  `Status` enum('Processing','Shipped','Delivered','Cancelled') NOT NULL DEFAULT 'Processing',
  `TotalAmount` decimal(10,2) NOT NULL COMMENT 'Загальна сума (товари після знижок)',
  `ShippingCost` decimal(10,2) NOT NULL DEFAULT '0.00' COMMENT 'Вартість доставки',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`OrderId`),
  UNIQUE KEY `OrderNumber` (`OrderNumber`),
  KEY `idx_user` (`UserId`),
  KEY `idx_order_number` (`OrderNumber`),
  KEY `idx_status` (`Status`),
  KEY `idx_created` (`CreatedAt`),
  CONSTRAINT `orders_ibfk_1` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orders`
--

LOCK TABLES `orders` WRITE;
/*!40000 ALTER TABLE `orders` DISABLE KEYS */;
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
  `Quantity` int NOT NULL COMMENT 'Кількість списання зі складу',
  `OrderId` int DEFAULT NULL COMMENT 'Якщо списання через замовлення',
  `CompanyId` int DEFAULT NULL COMMENT 'Фірма-постачальник (обов''язкове для повернень)',
  `Reason` enum('Order','Damaged','Lost','Return','Inventory') NOT NULL COMMENT 'Причина списання',
  `OriginalPrice` decimal(10,2) DEFAULT NULL COMMENT 'Базова ціна товару (тільки для Reason=Order)',
  `AppliedPromotionId` int DEFAULT NULL COMMENT 'ID застосованої знижки (тільки для Reason=Order)',
  `DiscountAmount` decimal(10,2) DEFAULT NULL COMMENT 'Сума знижки в грн (тільки для Reason=Order)',
  `FinalPrice` decimal(10,2) DEFAULT NULL COMMENT 'Фінальна ціна після знижки (тільки для Reason=Order)',
  `DocumentDate` date NOT NULL,
  `CreatedBy` int DEFAULT NULL COMMENT 'Менеджер/Адміністратор або NULL (авто)',
  `Notes` text,
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`DocumentId`),
  KEY `idx_product` (`ProductId`),
  KEY `idx_order` (`OrderId`),
  KEY `idx_company` (`CompanyId`),
  KEY `idx_promotion` (`AppliedPromotionId`),
  KEY `idx_date` (`DocumentDate`),
  KEY `idx_reason` (`Reason`),
  KEY `CreatedBy` (`CreatedBy`),
  CONSTRAINT `outgoingdocuments_ibfk_1` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE RESTRICT,
  CONSTRAINT `outgoingdocuments_ibfk_2` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE SET NULL,
  CONSTRAINT `outgoingdocuments_ibfk_3` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`CompanyId`) ON DELETE RESTRICT,
  CONSTRAINT `outgoingdocuments_ibfk_4` FOREIGN KEY (`AppliedPromotionId`) REFERENCES `promotions` (`PromotionId`) ON DELETE SET NULL,
  CONSTRAINT `outgoingdocuments_ibfk_5` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserId`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `outgoingdocuments`
--

LOCK TABLES `outgoingdocuments` WRITE;
/*!40000 ALTER TABLE `outgoingdocuments` DISABLE KEYS */;
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
  `TransactionId` varchar(255) DEFAULT NULL COMMENT 'ID транзакції Stripe',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`PaymentId`),
  UNIQUE KEY `OrderId` (`OrderId`),
  KEY `idx_order` (`OrderId`),
  KEY `idx_status` (`Status`),
  KEY `idx_transaction` (`TransactionId`),
  CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `payments`
--

LOCK TABLES `payments` WRITE;
/*!40000 ALTER TABLE `payments` DISABLE KEYS */;
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
  `ImageURL` varchar(500) NOT NULL COMMENT 'URL зображення в MinIO Storage',
  `IsPrimary` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Головне зображення товару',
  `DisplayOrder` int NOT NULL DEFAULT '0' COMMENT 'Порядок відображення',
  PRIMARY KEY (`ImageId`),
  KEY `idx_product` (`ProductId`),
  KEY `idx_primary` (`IsPrimary`),
  CONSTRAINT `productimages_ibfk_1` FOREIGN KEY (`ProductId`) REFERENCES `products` (`ProductId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
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
  `Description` text NOT NULL,
  `Price` decimal(10,2) NOT NULL COMMENT 'Ціна в грн',
  `Weight` decimal(10,3) NOT NULL COMMENT 'Вага в кг для розрахунку доставки',
  `CategoryId` int NOT NULL,
  `Stock` int NOT NULL DEFAULT '0' COMMENT 'Поточний залишок на складі (розраховується автоматично)',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`ProductId`),
  KEY `idx_name` (`Name`),
  KEY `idx_category` (`CategoryId`),
  KEY `idx_stock` (`Stock`),
  KEY `idx_price` (`Price`),
  CONSTRAINT `products_ibfk_1` FOREIGN KEY (`CategoryId`) REFERENCES `categories` (`CategoryId`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `products`
--

LOCK TABLES `products` WRITE;
/*!40000 ALTER TABLE `products` DISABLE KEYS */;
INSERT INTO `products` VALUES (1,'Футболка ХДУ класична біла','Класична біла футболка з логотипом Херсонського державного університету. Матеріал: 100% бавовна. Розміри: S, M, L, XL, XXL. Висока якість друку, стійкий до прання.',450.00,0.200,4,15,'2026-02-11 05:59:34','2026-02-11 06:38:26'),(2,'Футболка ХДУ синя з гербом','Синя футболка преміум-якості з великим гербом університету на грудях. Матеріал: бавовна з еластаном для зручної посадки. Ідеальна для студентських заходів.',450.00,0.220,4,11,'2026-02-11 05:59:34','2026-02-11 06:38:26'),(3,'Худі ХДУ чорне унісекс','Тепле чорне худі з капюшоном та кишенею-кенгуру. Логотип університету вишитий на грудях. Матеріал: 80% бавовна, 20% поліестер. Утеплений флісом всередині.',800.00,0.650,5,0,'2026-02-11 05:59:34',NULL),(4,'Худі ХДУ сіре з великим принтом','Стильне сіре худі oversize з великим принтом \"KHERSON STATE UNIVERSITY\" на спині. Комфортний крій, м\'яка тканина. Ідеальне для прохолодної погоди.',800.00,0.700,5,8,'2026-02-11 05:59:34','2026-02-11 06:38:26'),(5,'Кружка ХДУ керамічна класична','Біла керамічна кружка об\'ємом 350 мл з логотипом ХДУ. Можна мити в посудомийній машині та використовувати в мікрохвильовці. Якісний друк не стирається.',200.00,0.350,6,22,'2026-02-11 05:59:34','2026-02-11 06:38:26'),(6,'Термокружка ХДУ з кришкою','Термокружка з нержавіючої сталі об\'ємом 450 мл. Зберігає температуру до 6 годин. Герметична кришка, зручна для подорожей. Лазерне гравірування логотипу.',450.00,0.280,6,12,'2026-02-11 05:59:34','2026-02-11 06:38:26'),(7,'Блокнот ХДУ А5 в клітинку','Блокнот формату А5 на 96 аркушів в клітинку. Тверда обкладинка з тисненням логотипу. Зручна закладка-ляссе. Папір 80 г/м². Ідеальний для конспектів.',95.00,0.250,3,3,'2026-02-11 05:59:34','2026-02-11 06:38:26'),(8,'Ручка ХДУ металева в футлярі','Преміальна металева ручка з гравіруванням \"KHERSON STATE UNIVERSITY\". Поставляється в подарунковому футлярі. Синє чорнило, змінний стрижень. Відмінний подарунок.',30.00,0.080,3,0,'2026-02-11 05:59:34','2026-02-11 06:33:37');
/*!40000 ALTER TABLE `products` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `promotions`
--

DROP TABLE IF EXISTS `promotions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `promotions` (
  `PromotionId` int NOT NULL AUTO_INCREMENT COMMENT 'Первинний ключ акції/знижки',
  `Name` varchar(200) NOT NULL COMMENT 'Назва акції',
  `Description` text COMMENT 'Опис акції',
  `Type` enum('PERCENTAGE','FIXED_AMOUNT','SPECIAL_PRICE') NOT NULL COMMENT 'Тип знижки',
  `Value` decimal(10,2) NOT NULL COMMENT 'Значення знижки (%, грн або спеціальна ціна)',
  `TargetType` enum('PRODUCT','CATEGORY','CART','SHIPPING') NOT NULL COMMENT 'Область застосування',
  `TargetId` int DEFAULT NULL COMMENT 'ID товару або категорії (NULL для CART/SHIPPING)',
  `AudienceType` enum('ALL','STUDENTS','STAFF','ALUMNI','CUSTOM','NONE','REGULAR','SCHOLARSHIP','HIGH_ACHIEVER') NOT NULL DEFAULT 'ALL',
  `StartDate` timestamp NULL DEFAULT NULL COMMENT 'Дата початку дії (NULL = без обмеження)',
  `EndDate` timestamp NULL DEFAULT NULL COMMENT 'Дата закінчення дії (NULL = без обмеження)',
  `PromoCode` varchar(50) DEFAULT NULL COMMENT 'Промокод для активації (опціонально)',
  `MinOrderAmount` decimal(10,2) DEFAULT NULL COMMENT 'Мінімальна сума замовлення для застосування',
  `MinQuantity` int DEFAULT NULL COMMENT 'Мінімальна кількість товару для застосування',
  `Priority` int NOT NULL DEFAULT '0' COMMENT 'Пріоритет застосування (вищий = важливіший)',
  `UsageLimit` int DEFAULT NULL COMMENT 'Ліміт використань (NULL = необмежено)',
  `CurrentUsage` int NOT NULL DEFAULT '0' COMMENT 'Поточна кількість використань',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Чи активна акція',
  `CreatedBy` int NOT NULL COMMENT 'Менеджер, який створив акцію',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`PromotionId`),
  UNIQUE KEY `PromoCode` (`PromoCode`),
  KEY `idx_name` (`Name`),
  KEY `idx_type` (`Type`),
  KEY `idx_target_type` (`TargetType`),
  KEY `idx_audience` (`AudienceType`),
  KEY `idx_promo_code` (`PromoCode`),
  KEY `idx_active` (`IsActive`),
  KEY `idx_dates` (`StartDate`,`EndDate`),
  KEY `idx_priority` (`Priority`),
  KEY `CreatedBy` (`CreatedBy`),
  CONSTRAINT `promotions_ibfk_1` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `promotions`
--

LOCK TABLES `promotions` WRITE;
/*!40000 ALTER TABLE `promotions` DISABLE KEYS */;
INSERT INTO `promotions` VALUES (2,'Знижка для студентів REGULAR','Постійна знижка 5% для студентів зі статусом REGULAR на весь асортимент','PERCENTAGE',5.00,'CART',NULL,'REGULAR',NULL,NULL,NULL,NULL,NULL,10,NULL,0,1,2,'2026-02-11 10:07:08','2026-02-11 10:07:08'),(3,'Знижка для студентів SCHOLARSHIP','Постійна знижка 10% для студентів зі статусом SCHOLARSHIP на весь асортимент','PERCENTAGE',10.00,'CART',NULL,'SCHOLARSHIP',NULL,NULL,NULL,NULL,NULL,20,NULL,0,1,2,'2026-02-11 10:07:08','2026-02-11 10:07:08'),(4,'Знижка для студентів HIGH_ACHIEVER','Постійна знижка 15% для студентів зі статусом HIGH_ACHIEVER на весь асортимент','PERCENTAGE',15.00,'CART',NULL,'HIGH_ACHIEVER',NULL,NULL,NULL,NULL,NULL,30,NULL,0,1,2,'2026-02-11 10:07:08','2026-02-11 10:07:08'),(5,'Промокод KSU2026','Спеціальна знижка 5% за промокодом KSU2026 на весь асортимент','PERCENTAGE',5.00,'CART',NULL,'ALL',NULL,NULL,'KSU2026',NULL,NULL,5,NULL,0,1,2,'2026-02-11 10:07:08','2026-02-11 10:07:08');
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
  `OrderId` int NOT NULL COMMENT 'Один запис доставки на замовлення',
  `City` varchar(100) NOT NULL COMMENT 'Населений пункт Nova Poshta',
  `CityRef` varchar(100) DEFAULT NULL COMMENT 'Ref міста з Nova Poshta API',
  `WarehouseNumber` varchar(50) NOT NULL COMMENT 'Номер відділення Nova Poshta',
  `WarehouseRef` varchar(100) DEFAULT NULL COMMENT 'Ref відділення з Nova Poshta API',
  `RecipientName` varchar(100) NOT NULL COMMENT 'Ім''я отримувача',
  `RecipientPhone` varchar(20) NOT NULL COMMENT 'Телефон отримувача',
  `TrackingNumber` varchar(100) DEFAULT NULL COMMENT 'ТТН Nova Poshta',
  PRIMARY KEY (`ShippingId`),
  UNIQUE KEY `OrderId` (`OrderId`),
  KEY `idx_order` (`OrderId`),
  CONSTRAINT `shipping_ibfk_1` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`OrderId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `shipping`
--

LOCK TABLES `shipping` WRITE;
/*!40000 ALTER TABLE `shipping` DISABLE KEYS */;
/*!40000 ALTER TABLE `shipping` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `userpromotions`
--

DROP TABLE IF EXISTS `userpromotions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `userpromotions` (
  `UserPromotionId` int NOT NULL AUTO_INCREMENT COMMENT 'Первинний ключ зв''язку',
  `UserId` int NOT NULL COMMENT 'Користувач',
  `PromotionId` int NOT NULL COMMENT 'Акція/знижка',
  `AssignedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Дата призначення знижки',
  `UsedCount` int NOT NULL DEFAULT '0' COMMENT 'Кількість використань цієї знижки користувачем',
  PRIMARY KEY (`UserPromotionId`),
  UNIQUE KEY `unique_user_promotion` (`UserId`,`PromotionId`) COMMENT 'Унікальна пара: користувач-акція',
  KEY `idx_user` (`UserId`),
  KEY `idx_promotion` (`PromotionId`),
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
  `UserId` int NOT NULL AUTO_INCREMENT COMMENT 'Первинний ключ користувача',
  `FirstName` varchar(50) NOT NULL,
  `LastName` varchar(50) NOT NULL,
  `Email` varchar(100) NOT NULL,
  `Password` varchar(255) NOT NULL COMMENT 'Хешований пароль (bcrypt)',
  `Phone` varchar(20) DEFAULT NULL,
  `Role` enum('Guest','Customer','Manager','Administrator','SuperAdmin') NOT NULL DEFAULT 'Customer',
  `StudentStatus` enum('NONE','REGULAR','SCHOLARSHIP','HIGH_ACHIEVER') NOT NULL DEFAULT 'NONE' COMMENT 'Статус студента для знижок',
  `GPA` decimal(3,2) DEFAULT NULL COMMENT 'Середній бал (Grade Point Average), від 0.00 до 5.00',
  `StudentVerifiedAt` timestamp NULL DEFAULT NULL COMMENT 'Дата верифікації студентського статусу через University API',
  `StudentExpiresAt` timestamp NULL DEFAULT NULL COMMENT 'Дата закінчення студентського статусу (кінець семестру)',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `Email` (`Email`),
  KEY `idx_email` (`Email`),
  KEY `idx_role` (`Role`),
  KEY `idx_student_status` (`StudentStatus`),
  KEY `idx_student_expires` (`StudentExpiresAt`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'Адміністратор','Системи','admin@university.ks.ua','$2b$12$FIGyzdX5j3cGSfZQdY5zjeGR40lSqx51Q4G7aUd.KGuwbW6WGXMZC',NULL,'Administrator','NONE',NULL,NULL,NULL,'2026-02-11 05:50:12'),(2,'Менеджер','Магазину','manager@university.ks.ua','$2b$12$eaTf0o/lZJtggqx6TDuNp.ymtaoUgPOGJHjsiAB42tApObYofacze',NULL,'Manager','NONE',NULL,NULL,NULL,'2026-02-11 05:50:12'),(3,'Олександр','Петренко','petrenko@university.ks.ua','$2b$12$UDinljSJfa4vx5VwAa47U.XU5U2WP1jh9gKc54ekG4lIWNrHLFReO',NULL,'Customer','REGULAR',3.50,'2026-02-11 05:50:12','2026-06-11 04:50:12','2026-02-11 05:50:12'),(4,'Марія','Коваленко','kovalenko@university.ks.ua','$2b$12$UDinljSJfa4vx5VwAa47U.XU5U2WP1jh9gKc54ekG4lIWNrHLFReO',NULL,'Customer','SCHOLARSHIP',4.20,'2026-02-11 05:50:12','2026-06-11 04:50:12','2026-02-11 05:50:12'),(5,'Дмитро','Шевченко','shevchenko@university.ks.ua','$2b$12$UDinljSJfa4vx5VwAa47U.XU5U2WP1jh9gKc54ekG4lIWNrHLFReO',NULL,'Customer','HIGH_ACHIEVER',4.80,'2026-02-11 05:50:12','2026-06-11 04:50:12','2026-02-11 05:50:12'),(6,'Іван','Мельник','melnyk@gmail.com','$2b$12$UDinljSJfa4vx5VwAa47U.XU5U2WP1jh9gKc54ekG4lIWNrHLFReO',NULL,'Customer','NONE',NULL,NULL,NULL,'2026-02-11 05:50:12'),(7,'AutoTest','Student','autotest@university.ks.ua','$2a$12$sVi3uWM4IGApkUS/d9nmEe4dzNWb8oR3eBRJmgIskyFzr5yjoW8Xu','000','Customer','REGULAR',NULL,'2026-02-11 08:35:36','2027-02-11 08:35:36','2026-02-11 08:35:36');
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

-- Dump completed on 2026-02-17 10:50:07
