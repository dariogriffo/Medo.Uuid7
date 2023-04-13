Medo.Uuid7
==========

This project is implementation of UUID version 7 algorithm as defined in
[New UUID Formats draft 03 RFC][rfc_4122bis].

You can find packaged library at [NuGet][nuget_uuid7]
and add it you your application using the following command:

    dotnet add package Uuid7


## Format

The format of UUIDv7 is as specified below.

     0                   1                   2                   3
     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |                           unix_ts_ms                          |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |          unix_ts_ms           |  ver  |       rand_a          |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |var|                        rand_b                             |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |                            rand_b                             |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

*unix_tx_ms*:
48 bit big-endian unsigned number of Unix epoch timestamp.

*ver*:
4 bit UUIDv7 version. Always `0111`.

*rand_a*:
12 bits of pseudo-random data.

*var*:
2 bit variant. Always `10`.

*rand_b*:
Additional 62 bits of pseudo-random data.


### Implementation

As monotonicity is important for UUID version 7 generation, this implementation
implements most of [monotonic random counter][rfc_4122bis#counters]
recommendations.

Implementation uses randomly seeded 26 bit monotonic counter (25 random bits + 1
rollover guard bit) with a 4-bit increment.

Counter uses 12-bits from rand_a field and it "steals" 14 bits from rand_b
field. Counter will have its 25 bits fully randomized each millisecond tick.
Within the same millisecond tick, counter will be randomly increased using 4 bit
increment.

In the case of multithreaded use, the counter seed is different for each thread.

In the worst case, this implementation guarantees at least 2^21 monotonically
increasing UUIDs per millisecond. Up to 2^23 monotonically increasing UUID
values per millisecond can be expected on average. Monotonic increase for each
generated value is guaranteed on per thread basis.

The last 48 bits are filled with random data that is different for each
generated UUID.

As each UUID uses 48 random bits in addition to 25 random bits from the seeded
counter, this means we have at least 73 bits of entropy (without taking 48-bit
timestamp into account).

With those implementation details in mind, the final layout is defined as below.

     0                   1                   2                   3
     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |                           unix_ts_ms                          |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |          unix_ts_ms           |  ver  |        counter        |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |var|          counter          |            random             |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    |                            random                             |
    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

*unix_tx_ms*:
48 bit big-endian unsigned number of Unix epoch timestamp.

*ver*:
4 bit UUIDv7 version. Always `0111`.

*var*:
2 bit variant. Always `10`.

*counter*:
26 bit big-endian unsigned counter.

*random*:
48 bits of random data.


## Textual Representation

While this UUID should be handled and stored in its binary 128 bit form, it's
often useful to provide a textual representation.


### UUID Format

This is a standard hexadecimal representation of UUID with dashes separating
various components. Please note that this component separation doesn't
necessarily correlate with any internal fields.

Example:

    0185aee1-4413-7023-9109-bde493efe31d


### Id25

Alternative string representation is Id25 (Base-35), courtesy of [stevesimmons][git_stevesimmons_uuid7].
While I have seen similar encodings used before, his implementation is the first
one I saw being used on UUIDs. Since it uses only numbers and lowercase
characters, it actually retains lexicographical sorting property the default
UUID text format has.

UUID will always fit in 25 characters.

Example:

    0672016s27hx3fjxmn5ic1hzq


### Id22

If more compact string representation is needed, one can use Id22 (Base-58)
encoding. This is the same encoding Bitcoin uses for its keys.

UUID will always fit in 22 characters.

Example:

    1BuKkq6yWzmN2fCaHBjCRr



[rfc_4122bis]: https://www.ietf.org/archive/id/draft-ietf-uuidrev-rfc4122bis-03.html
[rfc_4122bis#counters]: https://www.ietf.org/archive/id/draft-ietf-uuidrev-rfc4122bis-03.html#name-monotonicity-and-counters
[nuget_uuid7]: https://www.nuget.org/packages/Uuid7/
[git_stevesimmons_uuid7]: https://github.com/stevesimmons/uuid7-csharp/
