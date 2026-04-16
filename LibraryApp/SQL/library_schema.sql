-- ============================================================
-- LibraryManager Pro — Database Schema
-- ============================================================
-- Run this script once to create the database and tables:
--   mysql -u root -p < library_schema.sql
-- ============================================================
 
CREATE DATABASE IF NOT EXISTS library_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE library_db;
 
CREATE TABLE IF NOT EXISTS books (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    author VARCHAR(255) NOT NULL,
    -- ISBN stored as NULL when not provided so the UNIQUE constraint
    -- does not collide for books that have no ISBN.
    isbn VARCHAR(20) NULL DEFAULT NULL,
    publication_year SMALLINT NULL,
    genre VARCHAR(100) NULL,
    shelf VARCHAR(50) NULL,
    `row_number` VARCHAR(50) NULL,
    is_available TINYINT(1) NOT NULL DEFAULT 1,
    cover_url VARCHAR(512) NULL,
    description TEXT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_title(title),
    INDEX idx_author(author),
    INDEX idx_isbn(isbn),
    INDEX idx_genre(genre),
    -- UNIQUE on isbn: MySQL allows multiple NULL values in a UNIQUE column,
    -- so books with no ISBN (stored as NULL) will not conflict.
    UNIQUE KEY uq_isbn(isbn)
) ENGINE=InnoDB;
 
CREATE TABLE IF NOT EXISTS borrow_records (
    id INT AUTO_INCREMENT PRIMARY KEY,
    book_id INT NOT NULL,
    borrower_name VARCHAR(255) NOT NULL,
    borrower_email VARCHAR(255) NOT NULL,
    borrow_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    due_date DATETIME NOT NULL,
    return_date DATETIME NULL,
    FOREIGN KEY (book_id) REFERENCES books(id) ON DELETE CASCADE
) ENGINE=InnoDB;
 
-- Sample data
INSERT IGNORE INTO books (title,author,isbn,publication_year,genre,shelf,`row_number`,is_available) VALUES
('The Pragmatic Programmer','David Thomas','9780135957059',1999,'Technology','A','1',1),
('Clean Code','Robert C. Martin','9780132350884',2008,'Technology','A','2',1),
('Design Patterns','Gang of Four','9780201633610',1994,'Technology','A','3',1),
('Harry Potter and the Philosopher''s Stone','J.K. Rowling','9780747532743',1997,'Fantasy','B','1',1),
('The Lord of the Rings','J.R.R. Tolkien','9780618640157',1954,'Fantasy','B','2',1),
('1984','George Orwell','9780451524935',1949,'Dystopia','C','1',1),
('Brave New World','Aldous Huxley','9780060850524',1932,'Dystopia','C','2',1),
('The Great Gatsby','F. Scott Fitzgerald','9780743273565',1925,'Classic','D','1',1),
('To Kill a Mockingbird','Harper Lee','9780061935466',1960,'Classic','D','2',1),
('Dune','Frank Herbert','9780441013593',1965,'Sci-Fi','E','1',1);