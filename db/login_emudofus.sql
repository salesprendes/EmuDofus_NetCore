/*
 Navicat Premium Dump SQL

 Source Server         : local
 Source Server Type    : MySQL
 Source Server Version : 80407 (8.4.7)
 Source Host           : localhost:3306
 Source Schema         : login_emudofus

 Target Server Type    : MySQL
 Target Server Version : 80407 (8.4.7)
 File Encoding         : 65001

 Date: 22/05/2026 00:28:57
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for account
-- ----------------------------
DROP TABLE IF EXISTS `account`;
CREATE TABLE `account`  (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Name` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  `Pseudo` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  `Password` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  `Power` int NOT NULL,
  `CreationDate` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `LastConnectionDate` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `LastConnectionIP` varchar(16) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  `RemainingSubscription` datetime NOT NULL DEFAULT '2000-01-01 00:00:00',
  `Banned` tinyint(1) NOT NULL,
  `Question` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  `Response` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE = MyISAM AUTO_INCREMENT = 17 CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of account
-- ----------------------------
INSERT INTO `account` VALUES (1, 'test', 'AIdemu', 'test', 1000, '2026-05-08 19:30:10', '2026-05-22 00:08:01', '127.0.0.1', '2026-05-08 20:30:13', 0, 'test', 'test');
INSERT INTO `account` VALUES (2, 'test2', 'test', 'test', 1000, '2000-01-01 00:00:00', '2026-05-19 22:09:49', '127.0.0.1', '2000-01-01 00:00:00', 0, '', '');

-- ----------------------------
-- Table structure for characterinstance
-- ----------------------------
DROP TABLE IF EXISTS `characterinstance`;
CREATE TABLE `characterinstance`  (
  `Id` bigint NOT NULL,
  `ServerId` int NOT NULL,
  `Name` varchar(20) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  `Breed` tinyint UNSIGNED NOT NULL,
  `Color1` int NOT NULL,
  `Color2` int NOT NULL,
  `Color3` int NOT NULL,
  `Skin` int NOT NULL,
  `SkinSize` int NOT NULL,
  `Vitality` int NOT NULL,
  `Wisdom` int NOT NULL,
  `Strength` int NOT NULL,
  `Intelligence` int NOT NULL,
  `Agility` int NOT NULL,
  `Chance` int NOT NULL,
  `Ap` int NOT NULL,
  `Mp` int NOT NULL,
  `Life` int NOT NULL,
  `Energy` int NOT NULL,
  `SpellPoint` int NOT NULL,
  `CaracPoint` int NOT NULL,
  `MapId` int NOT NULL,
  `CellId` int NOT NULL,
  `Restriction` int NOT NULL,
  `Experience` bigint NOT NULL,
  `AccountId` bigint NOT NULL,
  `Dead` tinyint(1) NOT NULL,
  `MaxLevel` int NOT NULL,
  `DeathCount` int NOT NULL,
  `Level` int NOT NULL,
  `Sex` tinyint(1) NOT NULL,
  `Kamas` bigint NOT NULL,
  `SavedMapId` int NOT NULL,
  `SavedCellId` int NOT NULL,
  `Merchant` bit(1) NOT NULL DEFAULT b'0',
  `TitleId` int NOT NULL DEFAULT 0,
  `TitleParams` varchar(100) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL DEFAULT '\"\"',
  `EmoteCapacity` int NOT NULL DEFAULT 0,
  `DeathType` int NOT NULL DEFAULT 0,
  `EquippedMount` int NOT NULL DEFAULT -1,
  `AlignmentId` int NOT NULL DEFAULT 0,
  `AlignmentLevel` int NOT NULL DEFAULT 0,
  `AlignmentPromotion` int NOT NULL DEFAULT 0,
  `AlignmentHonour` int NOT NULL DEFAULT 0,
  `AlignmentDishonour` int NOT NULL DEFAULT 0,
  `AlignmentEnabled` bit(1) NOT NULL DEFAULT b'0',
  `SerializedWaypoints` text CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_characterinstance_account`(`AccountId` ASC, `ServerId` ASC) USING BTREE,
  INDEX `IX_characterinstance_server`(`ServerId` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of characterinstance
-- ----------------------------
INSERT INTO `characterinstance` VALUES (10000, 1, 'Aidemu', 9, 14738701, 0, 0, 91, 100, 0, 0, 0, 0, 0, 0, 7, 3, 1050, 10000, 79, 995, 528, 186, 8192, 28915, 1, 0, 1, 0, 200, 1, 559675896, 10285, 219, b'0', 0, '', 1376255, 0, -1, 0, 0, 0, 0, 0, b'0', '7411,528');
INSERT INTO `characterinstance` VALUES (10001, 1, 'Test', 12, -1, -1, -1, 121, 100, 0, 0, 0, 0, 0, 0, 7, 3, 650, 10000, 119, 595, 8594, 122, 8192, 0, 2, 0, 1, 0, 120, 1, 0, 10289, 249, b'0', 0, '', 1376255, 0, -1, 0, 0, 0, 0, 0, b'0', '');

-- ----------------------------
-- Table structure for gameservers
-- ----------------------------
DROP TABLE IF EXISTS `gameservers`;
CREATE TABLE `gameservers`  (
  `Id` int NOT NULL,
  `Port` int NOT NULL DEFAULT 5555,
  `State` int NOT NULL DEFAULT 0,
  `Sub` int NOT NULL DEFAULT 0,
  `FreePlaces` int NOT NULL DEFAULT 0,
  `Ip` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '127.0.0.1',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE = MyISAM AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of gameservers
-- ----------------------------
INSERT INTO `gameservers` VALUES (1, 5555, 1, 0, 0, '127.0.0.1');

-- ----------------------------
-- Triggers structure for table characterinstance
-- ----------------------------
DROP TRIGGER IF EXISTS `characterinstance_after_insert`;
delimiter ;;
CREATE TRIGGER `characterinstance_after_insert` AFTER INSERT ON `characterinstance` FOR EACH ROW BEGIN
	DECLARE characterId BIGINT DEFAULT NEW.Id;
	DECLARE breedId TINYINT DEFAULT NEW.Breed;

	CALL game_emudofus.character_generate_spells(characterId, breedId);

	INSERT INTO game_emudofus.characteralignment VALUES (characterId, 0, 0, 0, 0, 0, 0);
	INSERT INTO game_emudofus.characterguild VALUES (characterId, -1, 0, 0, 0, 0);
END
;;
delimiter ;

-- ----------------------------
-- Triggers structure for table characterinstance
-- ----------------------------
DROP TRIGGER IF EXISTS `characterinstance_after_delete`;
delimiter ;;
CREATE TRIGGER `characterinstance_after_delete` AFTER DELETE ON `characterinstance` FOR EACH ROW BEGIN
	DELETE FROM game_emudofus.spellbookentry WHERE OwnerType = 0 AND OwnerId = OLD.Id;
END
;;
delimiter ;

SET FOREIGN_KEY_CHECKS = 1;
