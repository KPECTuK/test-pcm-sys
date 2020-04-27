-- definition

DROP TABLE IF EXISTS shapeTypes;
DROP TABLE IF EXISTS shapes;
DROP SEQUENCE IF EXISTS distributors_serial;

CREATE TABLE shapeTypes (
  Id char(4) CONSTRAINT PK_shapeTypes PRIMARY KEY
, ShapeType varchar NOT NULL
);

INSERT INTO shapeTypes ( Id , ShapeType )
VALUES
  ( 'circ', 'test_pcm_sys.DataCircle' )
, ( 'squr', 'test_pcm_sys.DataSquare' )
, ( 'rect', 'test_pcm_sys.DataRectangle' );

CREATE SEQUENCE distributors_serial START 1;

CREATE TABLE shapes (
  Id int DEFAULT nextval('distributors_serial')
, IdShape int NOT NULL CHECK ( IdShape > 0 )
, IdParent int NOT NULL DEFAULT 0
, IdShapeType char(4) NOT NULL
, ShapeArea numeric NOT NULL
, DateModify timestamp DEFAULT current_timestamp
, CONSTRAINT PK_shapes PRIMARY KEY ( Id )
);

CREATE UNIQUE INDEX IX_shape
ON shapes ( IdShape, IdParent, DateModify )
WHERE IdParent IS NULL;

INSERT INTO shapes ( IdShape, IdParent, IdShapeType, ShapeArea, DateModify )
VALUES
  ( 01, 0, 'rect', 5, TIMESTAMP '2020-04-10 01:00:00')
--
, ( 02,    01, 'squr', 5, TIMESTAMP '2020-04-10 01:00:00')
,    ( 05,    02, 'circ', 3, TIMESTAMP '2020-04-10 00:00:00')
,    ( 05,    02, 'circ', 4, TIMESTAMP '2020-04-10 01:00:00')
,    ( 05,    02, 'circ', 5, TIMESTAMP '2020-04-10 10:00:00') -- here
,    ( 06,    02, 'rect', 5, TIMESTAMP '2020-04-10 01:00:00')
--
, ( 03,    01, 'squr', 5, TIMESTAMP '2020-04-10 01:00:00')
,    ( 07,    03, 'circ', 5, TIMESTAMP '2020-04-10 01:00:00')
,    ( 08,    03, 'squr', 5, TIMESTAMP '2020-04-10 01:00:00')
,    ( 09,    03, 'circ', 5, TIMESTAMP '2020-04-10 10:00:00') -- here
--
, ( 04,    01, 'rect', 5, TIMESTAMP '2020-04-10 01:00:00')
,    ( 10,    04, 'circ', 5, TIMESTAMP '2020-04-10 01:00:00')
,    ( 11,    04, 'rect', 5, TIMESTAMP '2020-04-10 01:00:00');

-- обновление базы со стороны клиента можно написать с использованием MERGE

-- task: dot 2 part a.

SELECT
  t.*
FROM (
  SELECT
    t.*
  , max ( t.DateModify ) OVER ( PARTITION BY t.IdShape )
  FROM shapes t
) t
WHERE max = t.DateModify
ORDER BY t.IdParent, t.IdShape;

-- test

SELECT
  t.*
FROM shapes t
ORDER BY t.IdParent, t.IdShape;

-- task: dot 2 part b.

DROP FUNCTION IF EXISTS dot_2_part_b;

CREATE FUNCTION dot_2_part_b ( threshold timestamp )
RETURNS TABLE ( IdShape int, ShapeType varchar, ShapeArea numeric )
AS $$
BEGIN
  RETURN QUERY
  SELECT
    r.IdShape
  , h.ShapeType
  , r.ShapeArea
  FROM (
    SELECT
      t.IdShape
    , t.IdParent
    , t.DateModify
    , max ( t.DateModify ) OVER ( PARTITION BY t.IdShape )
    , t.IdShapeType shapeType
    , h.IdShapeType parentType
    , t.ShapeArea
    FROM shapes t
    LEFT JOIN shapes h
    ON h.IdShape = t.IdParent
    WHERE
          t.IdShapeType = 'circ'
      AND t.DateModify < threshold ) r
  LEFT JOIN shapetypes h
  ON h.Id = r.shapeType
  WHERE 
        r.DateModify = r.max
    AND r.parentType = 'squr';
END;
$$ LANGUAGE plpgsql;

-- test
SELECT * FROM dot_2_part_b ( timestamp '2020-04-10 10:00:00' );
