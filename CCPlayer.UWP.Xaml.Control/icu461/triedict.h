/**
 *******************************************************************************
 * Copyright (C) 2006, International Business Machines Corporation and others. *
 * All Rights Reserved.                                                        *
 *******************************************************************************
 */

#ifndef TRIEDICT_H
#define TRIEDICT_H

#include "utypes.h"
#include "uobject.h"
#include "utext.h"

struct UEnumeration;
struct UDataSwapper;
struct UDataMemory;

 /**
  * <p>UDataSwapFn function for use in swapping a compact dictionary.</p>
  *
  * @param ds Pointer to UDataSwapper containing global data about the
  *           transformation and function pointers for handling primitive
  *           types.
  * @param inData Pointer to the input data to be transformed or examined.
  * @param length Length of the data, counting bytes. May be -1 for preflighting.
  *               If length>=0, then transform the data.
  *               If length==-1, then only determine the length of the data.
  *               The length cannot be determined from the data itself for all
  *               types of data (e.g., not for simple arrays of integers).
  * @param outData Pointer to the output data buffer.
  *                If length>=0 (transformation), then the output buffer must
  *                have a capacity of at least length.
  *                If length==-1, then outData will not be used and can be NULL.
  * @param pErrorCode ICU UErrorCode parameter, must not be NULL and must
  *                   fulfill U_SUCCESS on input.
  * @return The actual length of the data.
  *
  * @see UDataSwapper
  */

U_CAPI int32_t U_EXPORT2
triedict_swap(const UDataSwapper *ds,
            const void *inData, int32_t length, void *outData,
            UErrorCode *pErrorCode);

U_NAMESPACE_BEGIN

class StringEnumeration;
struct CompactTrieHeader;

/*******************************************************************
 * TrieWordDictionary
 */

/**
 * <p>TrieWordDictionary is an abstract class that represents a word
 * dictionary based on a trie. The base protocol is read-only.
 * Subclasses may allow writing.</p>
 */
class U_COMMON_API TrieWordDictionary : public UMemory {
 public:

  /**
   * <p>Default constructor.</p>
   *
   */
  TrieWordDictionary();

  /**
   * <p>Virtual destructor.</p>
   */
  virtual ~TrieWordDictionary();

 /**
  * <p>Find dictionary words that match the text.</p>
  *
  * @param text A UText representing the text. The
  * iterator is left after the longest prefix match in the dictionary.
  * @param start The current position in text.
  * @param maxLength The maximum number of code units to match.
  * @param lengths An array that is filled with the lengths of words that matched.
  * @param count Filled with the number of elements output in lengths.
  * @param limit The size of the lengths array; this limits the number of words output.
  * @return The number of characters in text that were matched.
  */
  virtual int32_t matches( UText *text,
                              int32_t maxLength,
                              int32_t *lengths,
                              int &count,
                              int limit ) const = 0;

  /**
   * <p>Return a StringEnumeration for iterating all the words in the dictionary.</p>
   *
   * @param status A status code recording the success of the call.
   * @return A StringEnumeration that will iterate through the whole dictionary.
   * The caller is responsible for closing it. The order is unspecified.
   */
  virtual StringEnumeration *openWords( UErrorCode &status ) const = 0;

};

/*******************************************************************
 * MutableTrieDictionary
 */

/**
 * <p>MutableTrieDictionary is a TrieWordDictionary that allows words to be
 * added.</p>
 */

struct TernaryNode;             // Forwards declaration

class U_COMMON_API MutableTrieDictionary : public TrieWordDictionary {
 private:
    /**
     * The root node of the trie
     * @internal
     */

  TernaryNode               *fTrie;

    /**
     * A UText for internal use
     * @internal
     */

  UText    *fIter;

  friend class CompactTrieDictionary;   // For fast conversion

 public:

 /**
  * <p>Constructor.</p>
  *
  * @param median A UChar around which to balance the trie. Ideally, it should
  * begin at least one word that is near the median of the set in the dictionary
  * @param status A status code recording the success of the call.
  */
  MutableTrieDictionary( UChar median, UErrorCode &status );

  /**
   * <p>Virtual destructor.</p>
   */
  virtual ~MutableTrieDictionary();

 /**
  * <p>Find dictionary words that match the text.</p>
  *
  * @param text A UText representing the text. The
  * iterator is left after the longest prefix match in the dictionary.
  * @param maxLength The maximum number of code units to match.
  * @param lengths An array that is filled with the lengths of words that matched.
  * @param count Filled with the number of elements output in lengths.
  * @param limit The size of the lengths array; this limits the number of words output.
  * @return The number of characters in text that were matched.
  */
  virtual int32_t matches( UText *text,
                              int32_t maxLength,
                              int32_t *lengths,
                              int &count,
                              int limit ) const;

  /**
   * <p>Return a StringEnumeration for iterating all the words in the dictionary.</p>
   *
   * @param status A status code recording the success of the call.
   * @return A StringEnumeration that will iterate through the whole dictionary.
   * The caller is responsible for closing it. The order is unspecified.
   */
  virtual StringEnumeration *openWords( UErrorCode &status ) const;

 /**
  * <p>Add one word to the dictionary.</p>
  *
  * @param word A UChar buffer containing the word.
  * @param length The length of the word.
  * @param status The resultant status
  */
  virtual void addWord( const UChar *word,
                        int32_t length,
                        UErrorCode &status);

#if 0
 /**
  * <p>Add all strings from a UEnumeration to the dictionary.</p>
  *
  * @param words A UEnumeration that will return the desired words.
  * @param status The resultant status
  */
  virtual void addWords( UEnumeration *words, UErrorCode &status );
#endif

protected:
 /**
  * <p>Search the dictionary for matches.</p>
  *
  * @param text A UText representing the text. The
  * iterator is left after the longest prefix match in the dictionary.
  * @param maxLength The maximum number of code units to match.
  * @param lengths An array that is filled with the lengths of words that matched.
  * @param count Filled with the number of elements output in lengths.
  * @param limit The size of the lengths array; this limits the number of words output.
  * @param parent The parent of the current node
  * @param pMatched The returned parent node matched the input
  * @return The number of characters in text that were matched.
  */
  virtual int32_t search( UText *text,
                              int32_t maxLength,
                              int32_t *lengths,
                              int &count,
                              int limit,
                              TernaryNode *&parent,
                              UBool &pMatched ) const;

private:
 /**
  * <p>Private constructor. The root node it not allocated.</p>
  *
  * @param status A status code recording the success of the call.
  */
  MutableTrieDictionary( UErrorCode &status );
};

/*******************************************************************
 * CompactTrieDictionary
 */

/**
 * <p>CompactTrieDictionary is a TrieWordDictionary that has been compacted
 * to save space.</p>
 */
class U_COMMON_API CompactTrieDictionary : public TrieWordDictionary {
 private:
    /**
     * The root node of the trie
     */

  const CompactTrieHeader   *fData;

    /**
     * A UBool indicating whether or not we own the fData.
     */

  UBool                     fOwnData;

    UDataMemory              *fUData;
 public:
  /**
   * <p>Construct a dictionary from a UDataMemory.</p>
   *
   * @param data A pointer to a UDataMemory, which is adopted
   * @param status A status code giving the result of the constructor
   */
  CompactTrieDictionary(UDataMemory *dataObj, UErrorCode &status);

  /**
   * <p>Construct a dictionary from raw saved data.</p>
   *
   * @param data A pointer to the raw data, which is still owned by the caller
   * @param status A status code giving the result of the constructor
   */
  CompactTrieDictionary(const void *dataObj, UErrorCode &status);

  /**
   * <p>Construct a dictionary from a MutableTrieDictionary.</p>
   *
   * @param dict The dictionary to use as input.
   * @param status A status code recording the success of the call.
   */
  CompactTrieDictionary( const MutableTrieDictionary &dict, UErrorCode &status );

  /**
   * <p>Virtual destructor.</p>
   */
  virtual ~CompactTrieDictionary();

 /**
  * <p>Find dictionary words that match the text.</p>
  *
  * @param text A UText representing the text. The
  * iterator is left after the longest prefix match in the dictionary.
  * @param maxLength The maximum number of code units to match.
  * @param lengths An array that is filled with the lengths of words that matched.
  * @param count Filled with the number of elements output in lengths.
  * @param limit The size of the lengths array; this limits the number of words output.
  * @return The number of characters in text that were matched.
  */
  virtual int32_t matches( UText *text,
                              int32_t rangeEnd,
                              int32_t *lengths,
                              int &count,
                              int limit ) const;

  /**
   * <p>Return a StringEnumeration for iterating all the words in the dictionary.</p>
   *
   * @param status A status code recording the success of the call.
   * @return A StringEnumeration that will iterate through the whole dictionary.
   * The caller is responsible for closing it. The order is unspecified.
   */
  virtual StringEnumeration *openWords( UErrorCode &status ) const;

 /**
  * <p>Return the size of the compact data.</p>
  *
  * @return The size of the dictionary's compact data.
  */
  virtual uint32_t dataSize() const;
  
 /**
  * <p>Return a void * pointer to the compact data, platform-endian.</p>
  *
  * @return The data for the compact dictionary, suitable for passing to the
  * constructor.
  */
  virtual const void *data() const;
  
 /**
  * <p>Return a MutableTrieDictionary clone of this dictionary.</p>
  *
  * @param status A status code recording the success of the call.
  * @return A MutableTrieDictionary with the same data as this dictionary
  */
  virtual MutableTrieDictionary *cloneMutable( UErrorCode &status ) const;
  
 private:
 
  /**
   * <p>Convert a MutableTrieDictionary into a compact data blob.</p>
   *
   * @param dict The dictionary to convert.
   * @param status A status code recording the success of the call.
   * @return A single data blob starting with a CompactTrieHeader.
   */
  static CompactTrieHeader *compactMutableTrieDictionary( const MutableTrieDictionary &dict,
                                                        UErrorCode &status );

};

U_NAMESPACE_END

    /* TRIEDICT_H */
#endif
