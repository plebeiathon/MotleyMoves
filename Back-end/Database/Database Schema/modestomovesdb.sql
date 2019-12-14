-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

-- -----------------------------------------------------
-- Schema modestomovesdb
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema modestomovesdb
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `modestomovesdb` DEFAULT CHARACTER SET utf8 ;
USE `modestomovesdb` ;

-- -----------------------------------------------------
-- Table `modestomovesdb`.`Employees`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`Employees` (
  `employeeID` INT NOT NULL AUTO_INCREMENT,
  `lastName` VARCHAR(45) NOT NULL,
  `firstName` VARCHAR(45) NOT NULL,
  `position` VARCHAR(45) NOT NULL,
  `email` VARCHAR(45) NOT NULL,
  `password` VARCHAR(45) NOT NULL,
  `fullName` VARCHAR(90) NULL,
  PRIMARY KEY (`employeeID`),
  UNIQUE INDEX `email_UNIQUE` (`email` ASC))
ENGINE = InnoDB
COMMENT = 'The Employee table stores information for each employee.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`Members`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`Members` (
  `mermberID` INT NOT NULL AUTO_INCREMENT,
  `lastName` VARCHAR(45) NOT NULL,
  `firstName` VARCHAR(45) NOT NULL,
  `email` VARCHAR(45) NOT NULL,
  `phone` VARCHAR(45) NOT NULL,
  `password` VARCHAR(45) NOT NULL,
  `fullName` VARCHAR(90) NULL,
  PRIMARY KEY (`mermberID`),
  UNIQUE INDEX `email_UNIQUE` (`email` ASC))
ENGINE = InnoDB
COMMENT = 'The members table is where the information given by members is stored after signing up.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`Events`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`Events` (
  `eventID` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(45) NOT NULL,
  `startDate` DATE NOT NULL,
  `endDate` DATE NOT NULL,
  `employeeID` INT NOT NULL,
  `totalAttendees` INT NOT NULL,
  `description` LONGTEXT NULL,
  PRIMARY KEY (`eventID`),
  INDEX `employeeID_idx` (`employeeID` ASC),
  CONSTRAINT `event_employeeID`
    FOREIGN KEY (`employeeID`)
    REFERENCES `modestomovesdb`.`Employees` (`employeeID`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE)
ENGINE = InnoDB
COMMENT = 'The Events table stores all the information about each event added.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`EventAttendees`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`EventAttendees` (
  `eventID` INT NOT NULL,
  `attendeeName` INT NOT NULL,
  `checkIn` DATETIME NULL,
  `checkOut` DATETIME NULL,
  PRIMARY KEY (`eventID`, `attendeeName`),
  INDEX `memberID_idx` (`attendeeName` ASC),
  CONSTRAINT `ea_eventID`
    FOREIGN KEY (`eventID`)
    REFERENCES `modestomovesdb`.`Events` (`eventID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `ea_memberID`
    FOREIGN KEY (`attendeeName`)
    REFERENCES `modestomovesdb`.`Members` (`mermberID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
COMMENT = 'The EventAttendees table stores the members participation information for the event they sign up to attend and the time it takes for the member to finish the event.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`Programs`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`Programs` (
  `programID` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(45) NOT NULL,
  `classType` VARCHAR(45) NOT NULL,
  `description` MEDIUMTEXT NULL,
  PRIMARY KEY (`programID`))
ENGINE = InnoDB
COMMENT = 'The Programs table stores each type of program offered along with the description of the program.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`ProgramCalander`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`ProgramCalander` (
  `pcID` INT NOT NULL AUTO_INCREMENT,
  `prgramID` INT NOT NULL,
  `date` DATETIME NOT NULL,
  `totalAttendees` INT NULL,
  `employeeID` INT NOT NULL,
  PRIMARY KEY (`pcID`),
  INDEX `employeeID_idx` (`employeeID` ASC),
  INDEX `programID_idx` (`prgramID` ASC),
  CONSTRAINT `pc_employeeID`
    FOREIGN KEY (`employeeID`)
    REFERENCES `modestomovesdb`.`Employees` (`employeeID`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT `pc_programID`
    FOREIGN KEY (`prgramID`)
    REFERENCES `modestomovesdb`.`Programs` (`programID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
COMMENT = 'The ProgramCalander table stores each programs information centered around the date.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`ProgramAttendees`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`ProgramAttendees` (
  `pcID` INT NOT NULL,
  `attendeeName` INT NOT NULL,
  `checkIn` DATETIME NULL,
  `checkout` DATETIME NULL,
  PRIMARY KEY (`pcID`, `attendeeName`),
  INDEX `memberID_idx` (`attendeeName` ASC),
  CONSTRAINT `pa_pcID`
    FOREIGN KEY (`pcID`)
    REFERENCES `modestomovesdb`.`ProgramCalander` (`pcID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `pa_memberID`
    FOREIGN KEY (`attendeeName`)
    REFERENCES `modestomovesdb`.`Members` (`mermberID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
COMMENT = 'The ProgramAttendees table stores the member that has signed up for a specific program along with the check in and check out times.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`EventMap`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`EventMap` (
  `eventID` INT NOT NULL,
  `xCoordinate` DECIMAL(10,3) NOT NULL,
  `yCoordinate` DECIMAL(10,3) NOT NULL,
  PRIMARY KEY (`eventID`, `xCoordinate`, `yCoordinate`),
  CONSTRAINT `em_eventID`
    FOREIGN KEY (`eventID`)
    REFERENCES `modestomovesdb`.`Events` (`eventID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
COMMENT = 'The EventMap table stores individual x and y coordinates to be used on virtual maps.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`RewardType`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`RewardType` (
  `rtID` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(45) NOT NULL,
  `cost` DOUBLE(5,2) NOT NULL,
  `stock` INT NOT NULL,
  PRIMARY KEY (`rtID`))
ENGINE = InnoDB
COMMENT = 'The RewardType table stores the information about what rewards there are along with cost and amount in stock.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`Rewards`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`Rewards` (
  `memberID` INT NOT NULL,
  `rtID` INT NOT NULL,
  `dateGiven` DATE NOT NULL,
  PRIMARY KEY (`memberID`, `rtID`),
  INDEX `rtID_idx` (`rtID` ASC),
  CONSTRAINT `rewards_memberID`
    FOREIGN KEY (`memberID`)
    REFERENCES `modestomovesdb`.`Members` (`mermberID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `rewards_rtID`
    FOREIGN KEY (`rtID`)
    REFERENCES `modestomovesdb`.`RewardType` (`rtID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
COMMENT = 'The Rewards table stores the information of when a member receives any reward type along with the date they received it.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`ScholarshipTypes`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`ScholarshipTypes` (
  `stID` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(45) NOT NULL,
  `type` VARCHAR(45) NOT NULL,
  `awardAmount` DOUBLE(6,2) NOT NULL,
  PRIMARY KEY (`stID`))
ENGINE = InnoDB
COMMENT = 'The ScholarshipTypes table stores the information of each type of scholarship, its name, and the amount to be rewarded.';


-- -----------------------------------------------------
-- Table `modestomovesdb`.`AwardedScholarships`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `modestomovesdb`.`AwardedScholarships` (
  `memberID` INT NOT NULL,
  `stID` INT NOT NULL,
  `dateAwarded` DATE NOT NULL,
  PRIMARY KEY (`memberID`, `stID`),
  INDEX `stID_idx` (`stID` ASC),
  CONSTRAINT `as_memberID`
    FOREIGN KEY (`memberID`)
    REFERENCES `modestomovesdb`.`Members` (`mermberID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `as_stID`
    FOREIGN KEY (`stID`)
    REFERENCES `modestomovesdb`.`ScholarshipTypes` (`stID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
ENGINE = InnoDB
COMMENT = 'The AwardedScholarships table stores the information of when a member is rewarded a scholarship.';


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
