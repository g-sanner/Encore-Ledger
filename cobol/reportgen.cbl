      ******************************************************************
      *                                                                *
      * PROGRAM NAME: reportgen                                        *
      * PROGRAMMER:   GRACE SANNER                                     *
      * DUE DATE:     05/03/2026                                       *
      *                                                                *
      * FUNCTION: THIS PROGRAM COMPUTES TOTAL SALES DATA               *
      *           AND CREATES A SALES REPORT.                          *
      *                                                                *
      * INPUT:  A CSV FILE CONTAINING TRANSACTION DATE, DESCRIPTION,   *
      *         AMOUNT, AND CATEGORY NAME.                             *
      *                                                                *
      * OUTPUT: A JSON FILE CONTAINING PERIOD DATES, SUMMARY, AND      *
      *         CATEGORY BREAKDOWNS.                                   *
      *                                                                *
      * NOTES: BUILD: cobc -x -o reportgen.exe reportgen.cbl           *
      *        USAGE: reportgen.exe <input.csv> <output.json>          *
      *                                                                *
      ******************************************************************

       IDENTIFICATION DIVISION.
       PROGRAM-ID.    reportgen.
       AUTHOR.        GRACE SANNER.
       DATE-WRITTEN.  05/03/2026. 
       DATE-COMPILED. 05/03/2026. 

       ENVIRONMENT DIVISION.

       INPUT-OUTPUT SECTION.

       FILE-CONTROL.

           SELECT CSV-FILE ASSIGN TO DYNAMIC WS-CSV-PATH
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-CSV-STATUS.
           SELECT JSON-FILE ASSIGN TO DYNAMIC WS-JSON-PATH
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-JSON-STATUS.

       DATA DIVISION.

       FILE SECTION.

       FD  CSV-FILE.

       01  CSV-RECORD            PIC X(8192).

       FD  JSON-FILE.

       01  JSON-RECORD           PIC X(65535).

       WORKING-STORAGE SECTION.

       01  WS-ARG-COUNT          PIC 9(4).
       01  WS-CSV-PATH           PIC X(512).
       01  WS-JSON-PATH          PIC X(512).
       01  WS-CSV-STATUS         PIC XX.
       01  WS-JSON-STATUS        PIC XX.

       01  WS-HEADER-SKIPPED     PIC X VALUE "N".
       01  WS-TX-COUNT           PIC 9(9) VALUE 0.

       01  WS-MIN-DATE           PIC X(10) VALUE "9999-12-31".
       01  WS-MAX-DATE           PIC X(10) VALUE "0000-01-01".

       01  WS-TOTAL-INCOME       PIC S9(14)V99 VALUE 0.
       01  WS-TOTAL-EXPENSE      PIC S9(14)V99 VALUE 0.

       01  WS-CAT-TABLE.
           05  WS-CAT OCCURS 500 TIMES INDEXED BY IX IY IZ.
               10  WS-CAT-NAME    PIC X(128).
               10  WS-CAT-INCOME  PIC S9(14)V99 VALUE 0.
               10  WS-CAT-EXPENSE PIC S9(14)V99 VALUE 0.
       01  WS-CAT-COUNT          PIC 9(4) VALUE 0.

       01  WS-LINE               PIC X(8192).
       01  WS-DATE-S             PIC X(10).
       01  WS-DESC-S             PIC X(4000).
       01  WS-AMT-S              PIC X(40).
       01  WS-CAT-S              PIC X(128).
       01  WS-RAW                PIC S9(14)V99.
       01  WS-NET                PIC S9(14)V99.

       01  WS-FOUND              PIC X.
       01  WS-I                  PIC 9(4).
       01  WS-J                  PIC 9(4).
       01  WS-TMP-NAME           PIC X(128).
       01  WS-TMP-INC            PIC S9(14)V99.
       01  WS-TMP-EXP            PIC S9(14)V99.

       01  WS-NUM-BUF            PIC -(14)9.99.
       01  WS-JSON               PIC X(65535).
       01  WS-JSON-PTR           PIC 9(6).
       01  WS-CAT-JSON           PIC X(2048).
       01  WS-K                  PIC 9(4).

       PROCEDURE DIVISION.

       0000-MAIN.

           ACCEPT WS-ARG-COUNT FROM ARGUMENT-NUMBER.

           IF WS-ARG-COUNT < 2
               DISPLAY "USAGE: reportgen <input.csv>"
                   " <output.json>" UPON SYSERR
               MOVE 1 TO RETURN-CODE
               GOBACK
           END-IF.
           
           ACCEPT WS-CSV-PATH FROM ARGUMENT-VALUE.
           ACCEPT WS-JSON-PATH FROM ARGUMENT-VALUE.

           OPEN INPUT CSV-FILE.
           IF WS-CSV-STATUS NOT = "00"
               DISPLAY "Cannot open CSV: " WS-CSV-PATH
                   " status " WS-CSV-STATUS
                   UPON SYSERR
               MOVE 1 TO RETURN-CODE
               GOBACK
           END-IF.

           PERFORM UNTIL WS-CSV-STATUS NOT = "00"
               READ CSV-FILE
                   AT END CONTINUE
                   NOT AT END PERFORM 0100-PROCESS-CSV-LINE
               END-READ
           END-PERFORM.

           CLOSE CSV-FILE.

           IF WS-TX-COUNT = 0
               MOVE "1900-01-01" TO WS-MIN-DATE WS-MAX-DATE
           END-IF.

           PERFORM 1000-SORT-CATEGORIES.
           PERFORM 2000-WRITE-JSON.

           MOVE 0 TO RETURN-CODE.
           GOBACK.

       0100-PROCESS-CSV-LINE.

           MOVE CSV-RECORD TO WS-LINE.

           IF WS-HEADER-SKIPPED = "N"
               MOVE "Y" TO WS-HEADER-SKIPPED
               EXIT PARAGRAPH
           END-IF.

           PERFORM 0110-PARSE-CSV-LINE.

           IF WS-DATE-S = SPACES
               EXIT PARAGRAPH
           END-IF.

           ADD 1 TO WS-TX-COUNT.

           IF WS-DATE-S < WS-MIN-DATE
               MOVE WS-DATE-S TO WS-MIN-DATE
           END-IF.

           IF WS-DATE-S > WS-MAX-DATE
               MOVE WS-DATE-S TO WS-MAX-DATE
           END-IF.

           COMPUTE WS-RAW = FUNCTION NUMVAL-C(FUNCTION TRIM (WS-AMT-S)).

           IF WS-RAW > 0
               ADD WS-RAW TO WS-TOTAL-INCOME
           ELSE
               IF WS-RAW < 0
                   COMPUTE WS-NET = 0 - WS-RAW
                   ADD WS-NET TO WS-TOTAL-EXPENSE
               END-IF
           END-IF.

           PERFORM 0120-CREATE-UPDATE-CATEGORY.

       0110-PARSE-CSV-LINE.

           UNSTRING WS-LINE DELIMITED BY ","
               INTO WS-DATE-S WS-DESC-S WS-AMT-S WS-CAT-S
           END-UNSTRING.

           IF WS-CAT-S = SPACES
               MOVE "Uncategorized" TO WS-CAT-S
           END-IF.

       0120-CREATE-UPDATE-CATEGORY.

           MOVE "N" TO WS-FOUND.

           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > WS-CAT-COUNT
               IF WS-CAT-NAME (IX) = WS-CAT-S
                   MOVE "Y" TO WS-FOUND
                   IF WS-RAW > 0
                       ADD WS-RAW TO WS-CAT-INCOME (IX)
                   ELSE
                       IF WS-RAW < 0
                           COMPUTE WS-NET = 0 - WS-RAW
                           ADD WS-NET TO WS-CAT-EXPENSE (IX)
                       END-IF
                   END-IF
               END-IF
           END-PERFORM.

           IF WS-FOUND = "N"
               IF WS-CAT-COUNT >= 500
                   DISPLAY "Too many categories (max 500)" UPON SYSERR
                   STOP RUN RETURNING 1
               END-IF
               ADD 1 TO WS-CAT-COUNT
               MOVE WS-CAT-S TO WS-CAT-NAME (WS-CAT-COUNT)
               MOVE 0 TO WS-CAT-INCOME (WS-CAT-COUNT)
               MOVE 0 TO WS-CAT-EXPENSE (WS-CAT-COUNT)
               IF WS-RAW > 0
                   ADD WS-RAW TO WS-CAT-INCOME (WS-CAT-COUNT)
               ELSE
                   IF WS-RAW < 0
                       COMPUTE WS-NET = 0 - WS-RAW
                       ADD WS-NET TO WS-CAT-EXPENSE (WS-CAT-COUNT)
                   END-IF
               END-IF
           END-IF.

       1000-SORT-CATEGORIES.

           PERFORM VARYING WS-I FROM 1 BY 1
               UNTIL WS-I >= WS-CAT-COUNT
               ADD 1 TO WS-I GIVING WS-J
               PERFORM VARYING WS-J FROM WS-J BY 1
                   UNTIL WS-J > WS-CAT-COUNT
                   IF WS-CAT-NAME (WS-I) > WS-CAT-NAME (WS-J)
                       MOVE WS-CAT-NAME (WS-I) TO WS-TMP-NAME
                       MOVE WS-CAT-NAME (WS-J) TO WS-CAT-NAME (WS-I)
                       MOVE WS-TMP-NAME TO WS-CAT-NAME (WS-J)
                       MOVE WS-CAT-INCOME (WS-I) TO WS-TMP-INC
                       MOVE WS-CAT-INCOME (WS-J) TO WS-CAT-INCOME (WS-I)
                       MOVE WS-TMP-INC TO WS-CAT-INCOME (WS-J)
                       MOVE WS-CAT-EXPENSE (WS-I) TO WS-TMP-EXP
                       MOVE WS-CAT-EXPENSE (WS-J)
                           TO WS-CAT-EXPENSE (WS-I)
                       MOVE WS-TMP-EXP TO WS-CAT-EXPENSE (WS-J)
                   END-IF
               END-PERFORM
           END-PERFORM.

       2000-WRITE-JSON.

           COMPUTE WS-NET = WS-TOTAL-INCOME - WS-TOTAL-EXPENSE.

           MOVE 1 TO WS-JSON-PTR.

           STRING "{" DELIMITED BY SIZE
               '"periodStartDate":"' DELIMITED BY SIZE
               FUNCTION TRIM (WS-MIN-DATE) DELIMITED BY SIZE
               '","periodEndDate":"' DELIMITED BY SIZE
               FUNCTION TRIM (WS-MAX-DATE) DELIMITED BY SIZE
               '","summary":{' DELIMITED BY SIZE
               '"totalIncome":' DELIMITED BY SIZE
               INTO WS-JSON
               WITH POINTER WS-JSON-PTR
           END-STRING.

           MOVE WS-TOTAL-INCOME TO WS-NUM-BUF.
           STRING FUNCTION TRIM (WS-NUM-BUF) DELIMITED BY SIZE
               ',"totalExpenses":' DELIMITED BY SIZE
               INTO WS-JSON
               WITH POINTER WS-JSON-PTR
           END-STRING.

           MOVE WS-TOTAL-EXPENSE TO WS-NUM-BUF.
           STRING FUNCTION TRIM (WS-NUM-BUF) DELIMITED BY SIZE
               ',"netBalance":' DELIMITED BY SIZE
               INTO WS-JSON
               WITH POINTER WS-JSON-PTR
           END-STRING.

           MOVE WS-NET TO WS-NUM-BUF.
           STRING FUNCTION TRIM (WS-NUM-BUF) DELIMITED BY SIZE
               '},"categoryBreakdowns":[' DELIMITED BY SIZE
               INTO WS-JSON
               WITH POINTER WS-JSON-PTR
           END-STRING.

           PERFORM VARYING WS-K FROM 1 BY 1
               UNTIL WS-K > WS-CAT-COUNT
               COMPUTE WS-NET = WS-CAT-INCOME (WS-K)
                   - WS-CAT-EXPENSE (WS-K)
               STRING '{"categoryName":"' DELIMITED BY SIZE
                   FUNCTION TRIM (WS-CAT-NAME (WS-K)) DELIMITED BY SIZE
                   '","income":' DELIMITED BY SIZE
                   INTO WS-CAT-JSON
               END-STRING

               MOVE WS-CAT-INCOME (WS-K) TO WS-NUM-BUF
               STRING FUNCTION TRIM (WS-CAT-JSON) DELIMITED BY SIZE
                   FUNCTION TRIM (WS-NUM-BUF) DELIMITED BY SIZE
                   ',"expense":' DELIMITED BY SIZE
                   INTO WS-CAT-JSON
               END-STRING

               MOVE WS-CAT-EXPENSE (WS-K) TO WS-NUM-BUF
               STRING FUNCTION TRIM (WS-CAT-JSON) DELIMITED BY SIZE
                   FUNCTION TRIM (WS-NUM-BUF) DELIMITED BY SIZE
                   ',"net":' DELIMITED BY SIZE
                   INTO WS-CAT-JSON
               END-STRING

               MOVE WS-NET TO WS-NUM-BUF
               STRING FUNCTION TRIM (WS-CAT-JSON) DELIMITED BY SIZE
                   FUNCTION TRIM (WS-NUM-BUF) DELIMITED BY SIZE
                   '}' DELIMITED BY SIZE
                   INTO WS-CAT-JSON
               END-STRING

               IF WS-K > 1
                   STRING "," DELIMITED BY SIZE
                       INTO WS-JSON
                       WITH POINTER WS-JSON-PTR
                   END-STRING
               END-IF

               STRING FUNCTION TRIM (WS-CAT-JSON) DELIMITED BY SIZE
                   INTO WS-JSON
                   WITH POINTER WS-JSON-PTR
               END-STRING
           END-PERFORM.

           STRING "]}" DELIMITED BY SIZE
               INTO WS-JSON
               WITH POINTER WS-JSON-PTR
           END-STRING.

           OPEN OUTPUT JSON-FILE.

           IF WS-JSON-STATUS NOT = "00"
               DISPLAY "Cannot open JSON output: "
                   WS-JSON-STATUS UPON SYSERR
               MOVE 1 TO RETURN-CODE
               GOBACK
           END-IF.

           MOVE WS-JSON TO JSON-RECORD.
           WRITE JSON-RECORD.

           IF WS-JSON-STATUS NOT = "00"
               DISPLAY "Write JSON failed: " WS-JSON-STATUS UPON SYSERR
               CLOSE JSON-FILE
               MOVE 1 TO RETURN-CODE
               GOBACK
           END-IF.
           CLOSE JSON-FILE.

       END PROGRAM reportgen.
