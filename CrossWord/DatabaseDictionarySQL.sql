; -- find all words that only contain the letters a-å and no spaces
SELECT w.Value 
FROM Words AS w 
WHERE w.NumberOfWords = 1 
AND w.NumberOfLetters <= 23 
AND w.Value REGEXP '^[A-Å]+$'
ORDER BY w.Value COLLATE utf8mb4_da_0900_as_cs;

; -- separated into three queries 
SELECT `w`.`WordId`, `w`.`Value`
FROM `Words` AS `w`
WHERE `w`.`Value` IN ('SAMS', 'STADEL', 'ELIANA')
ORDER BY `w`.`WordId`;

SELECT `t`.`WordFromId`, `t`.`WordToId`, `t`.`Value`, `w`.`WordId`
FROM `Words` AS `w`
INNER JOIN (
    SELECT `w0`.`WordFromId`, `w0`.`WordToId`, `w1`.`WordId`, `w1`.`Value`
    FROM `WordRelations` AS `w0`
    INNER JOIN `Words` AS `w1` ON `w0`.`WordToId` = `w1`.`WordId`
) AS `t` ON `w`.`WordId` = `t`.`WordFromId`
WHERE `w`.`Value` IN ('SAMS', 'STADEL', 'ELIANA')
ORDER BY `w`.`WordId`;

SELECT `t`.`WordFromId`, `t`.`WordToId`, `t`.`Value`, `w`.`WordId`
FROM `Words` AS `w`
INNER JOIN (
    SELECT `w0`.`WordFromId`, `w0`.`WordToId`,`w1`.`WordId`, `w1`.`Value`
    FROM `WordRelations` AS `w0`
    INNER JOIN `Words` AS `w1` ON `w0`.`WordFromId` = `w1`.`WordId`
) AS `t` ON `w`.`WordId` = `t`.`WordToId`
WHERE `w`.`Value` IN ('SAMS', 'STADEL', 'ELIANA')
ORDER BY `w`.`WordId`;

; -- one query but too many columns
SELECT DISTINCT `w`.`WordId`, `w`.`Value`, `t`.`WordFromId`, `t`.`WordToId`, `t0`.`WordId`, `t0`.`Value`
FROM `Words` AS `w`
LEFT JOIN (
    SELECT `w0`.`WordFromId`, `w0`.`WordToId`, `w1`.`WordId`, `w1`.`Value`
    FROM `WordRelations` AS `w0`
    INNER JOIN `Words` AS `w1` ON `w0`.`WordToId` = `w1`.`WordId`
) AS `t` ON `w`.`WordId` = `t`.`WordFromId`
LEFT JOIN (
    SELECT `w2`.`WordFromId`, `w2`.`WordToId`, `w3`.`WordId`, `w3`.`Value`
    FROM `WordRelations` AS `w2`
    INNER JOIN `Words` AS `w3` ON `w2`.`WordFromId` = `w3`.`WordId`
) AS `t0` ON `w`.`WordId` = `t0`.`WordToId`
WHERE `w`.`Value` IN ('SAMS', 'STADEL', 'ELIANA')
ORDER BY `w`.`WordId`, `t`.`WordFromId`, `t`.`WordToId`, `t0`.`Value`;

; -- one query OK
SELECT DISTINCT `w`.`WordId`, `w`.`Value`, `t0`.`Value`
FROM `Words` AS `w`
LEFT JOIN (
    SELECT `w0`.`WordFromId`, `w0`.`WordToId`, `w1`.`WordId`, `w1`.`Value`
    FROM `WordRelations` AS `w0`
    INNER JOIN `Words` AS `w1` ON `w0`.`WordToId` = `w1`.`WordId`
) AS `t` ON `w`.`WordId` = `t`.`WordFromId`
LEFT JOIN (
    SELECT `w2`.`WordFromId`, `w2`.`WordToId`, `w3`.`WordId`, `w3`.`Value`
    FROM `WordRelations` AS `w2`
    INNER JOIN `Words` AS `w3` ON `w2`.`WordFromId` = `w3`.`WordId`
) AS `t0` ON `w`.`WordId` = `t0`.`WordToId`
WHERE `w`.`Value` IN ('SAMS', 'STADEL', 'ELIANA')
ORDER BY `w`.`WordId`, `w`.`Value`, `t0`.`Value`;

; -- test with many words
SELECT DISTINCT `w`.`WordId`, `w`.`Value`, `t0`.`Value`
FROM `Words` AS `w`
LEFT JOIN (
    SELECT `w0`.`WordFromId`, `w0`.`WordToId`, `w1`.`WordId`, `w1`.`Value`
    FROM `WordRelations` AS `w0`
    INNER JOIN `Words` AS `w1` ON `w0`.`WordToId` = `w1`.`WordId`
) AS `t` ON `w`.`WordId` = `t`.`WordFromId`
LEFT JOIN (
    SELECT `w2`.`WordFromId`, `w2`.`WordToId`, `w3`.`WordId`, `w3`.`Value`
    FROM `WordRelations` AS `w2`
    INNER JOIN `Words` AS `w3` ON `w2`.`WordFromId` = `w3`.`WordId`
) AS `t0` ON `w`.`WordId` = `t0`.`WordToId`
WHERE `w`.`Value` IN ('SAMS', 'STADEL', 'ELIANA', 'WIS', 'ARAT', 'STIDELE', 'SARGEN', 'EAT', 'MARA', 'KAMGRAN', 'SPRETNESTE', 'ESTETENE', 'ITT', 'OSER', 'DERN', 'NOONE', 'SNEIENES', 'MARNIE', 'ENSETE', 'AAN', 'RNN', 'SIPES', 'MIKKELSMESSELEITE', 'ULLRAGGE', 'AAA', 'ARER', 'EIN', 'SOUEN', 'LEAVED', 'VOR', 'VISENE', 'PENGESORGER', 'BASES', 'ELNES', 'DAGENE', 'NOR', 'OORT', 'SATO', 'KES', 'UTENDØRSARENA', 'TAO', 'TREA', 'EIRE', 'TAK', 'NATALE', 'BAREA', 'RINGE', 'SUVERENENES', 'SAARDE', 'EEE', 'SANAGA', 'SNARE', 'ARV', 'ALAN', 'GON', 'AGREERER', 'SEMULEGRYNSGRØTEN', 'LANEN', 'OUA', 'ØIA', 'BESTAS', 'SPIREA', 'LOCOVARE', 'STEKT', 'IONE', 'MARC', 'IRS', 'MARKERER', 'STENEMARKA', 'SOLBANE', 'TREE', 'TEN', 'PONGEE', 'TRENSEN', 'TORE', 'ETE', 'PANSER', 'AARNES', 'ARET', 'SAMENE', 'USPD', 'BSA', 'SISTE', 'ARASON', 'LOEAK', 'AARS', 'POTET', 'MARTOS', 'LUNGE', 'RAVELINENE', 'STAENE', 'REGESTER', 'MAREN', 'TET', 'ANEN', 'RADAUNE', 'EPP', 'SKE', 'EMG', 'SEUE', 'ELLEA', 'MOA', 'STAN', 'IGLO', 'TAR', 'AEN', 'MANN', 'TIME', 'AKEERNE', 'IENG', 'LARGS', 'ADG', 'SAK', 'AGONENE', 'ROORKEE', 'DERINNE', 'VERDIGE', 'YUCCAER', 'ELATE', 'LAER', 'ØRE', 'GNAO', 'LENTI', 'SAD', 'ORE', 'SOS', 'VISTA', 'ERMA', 'BOS', 'SANG', 'ARORA', 'ESSONNE', 'VARATUN', 'RØRSLER', 'LAPSENS', 'OSTRAVA', 'ØIE', 'BNN', 'IRRES', 'SARE', 'EKEGATA', 'MASE', 'AGER', 'SER', 'SSN', 'RAGE', 'ANES', 'NET', 'MILEV', 'AANE', 'RNB', 'REN', 'ANN', 'APERIET', 'ANSE', 'ESK', 'EDREI', 'SLOTTENE', 'STETTA', 'WESENSTEEN', 'AANAR', 'TERROR', 'IATRI', 'EINE', 'OLERE', 'AKEERE', 'STENE', 'NES', 'ESER', 'STREET')
ORDER BY `w`.`WordId`, `w`.`Value`, `t0`.`Value`;



; -- limit with many words
SELECT sub.WordId, sub.Value, sub.RelatedValue
FROM (
    SELECT
        w1.WordId,
        w1.Value,
        w2.Value AS RelatedValue,
        ROW_NUMBER() OVER (PARTITION BY w1.WordId ORDER BY w2.WordId) AS row_num
    FROM Words AS w1
    LEFT JOIN WordRelations AS wr1 ON w1.WordId = wr1.WordFromId
    LEFT JOIN Words AS w2 ON wr1.WordToId = w2.WordId
    WHERE w1.Value IN ('SAMS', 'STADEL', 'ELIANA', 'WIS', 'ARAT', 'STIDELE', 'SARGEN', 'EAT', 'MARA', 'KAMGRAN', 'SPRETNESTE', 'ESTETENE', 'ITT', 'OSER', 'DERN', 'NOONE', 'SNEIENES', 'MARNIE', 'ENSETE', 'AAN', 'RNN', 'SIPES', 'MIKKELSMESSELEITE', 'ULLRAGGE', 'AAA', 'ARER', 'EIN', 'SOUEN', 'LEAVED', 'VOR', 'VISENE', 'PENGESORGER', 'BASES', 'ELNES', 'DAGENE', 'NOR', 'OORT', 'SATO', 'KES', 'UTENDØRSARENA', 'TAO', 'TREA', 'EIRE', 'TAK', 'NATALE', 'BAREA', 'RINGE', 'SUVERENENES', 'SAARDE', 'EEE', 'SANAGA', 'SNARE', 'ARV', 'ALAN', 'GON', 'AGREERER', 'SEMULEGRYNSGRØTEN', 'LANEN', 'OUA', 'ØIA', 'BESTAS', 'SPIREA', 'LOCOVARE', 'STEKT', 'IONE', 'MARC', 'IRS', 'MARKERER', 'STENEMARKA', 'SOLBANE', 'TREE', 'TEN', 'PONGEE', 'TRENSEN', 'TORE', 'ETE', 'PANSER', 'AARNES', 'ARET', 'SAMENE', 'USPD', 'BSA', 'SISTE', 'ARASON', 'LOEAK', 'AARS', 'POTET', 'MARTOS', 'LUNGE', 'RAVELINENE', 'STAENE', 'REGESTER', 'MAREN', 'TET', 'ANEN', 'RADAUNE', 'EPP', 'SKE', 'EMG', 'SEUE', 'ELLEA', 'MOA', 'STAN', 'IGLO', 'TAR', 'AEN', 'MANN', 'TIME', 'AKEERNE', 'IENG', 'LARGS', 'ADG', 'SAK', 'AGONENE', 'ROORKEE', 'DERINNE', 'VERDIGE', 'YUCCAER', 'ELATE', 'LAER', 'ØRE', 'GNAO', 'LENTI', 'SAD', 'ORE', 'SOS', 'VISTA', 'ERMA', 'BOS', 'SANG', 'ARORA', 'ESSONNE', 'VARATUN', 'RØRSLER', 'LAPSENS', 'OSTRAVA', 'ØIE', 'BNN', 'IRRES', 'SARE', 'EKEGATA', 'MASE', 'AGER', 'SER', 'SSN', 'RAGE', 'ANES', 'NET', 'MILEV', 'AANE', 'RNB', 'REN', 'ANN', 'APERIET', 'ANSE', 'ESK', 'EDREI', 'SLOTTENE', 'STETTA', 'WESENSTEEN', 'AANAR', 'TERROR', 'IATRI', 'EINE', 'OLERE', 'AKEERE', 'STENE', 'NES', 'ESER', 'STREET')
) AS sub
WHERE sub.row_num <= 10;
