using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucs.Streams; 

/// <summary>Implements a <see cref="T:System.IO.TextWriter" /> for writing information to a string. The information is stored in an underlying <see cref="T:System.Text.StringBuilder" />.</summary>
[ComVisible(true)]
[Serializable]
public class StringBuilderTextWriter : TextWriter {
    private static volatile UnicodeEncoding m_encoding;
    private StringBuilder _sb;
    private bool _isOpen;

    public void Clear() {
        _sb.Length = 0;
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.IO.StringWriter" /> class.</summary>
    public StringBuilderTextWriter()
        : this(new StringBuilder(), (IFormatProvider) CultureInfo.CurrentCulture) { }

    /// <summary>Initializes a new instance of the <see cref="T:System.IO.StringWriter" /> class with the specified format control.</summary>
    /// <param name="formatProvider">An <see cref="T:System.IFormatProvider" /> object that controls formatting.</param>
    public StringBuilderTextWriter(IFormatProvider formatProvider)
        : this(new StringBuilder(), formatProvider) { }

    /// <summary>Initializes a new instance of the <see cref="T:System.IO.StringWriter" /> class that writes to the specified <see cref="T:System.Text.StringBuilder" />.</summary>
    /// <param name="sb">The <see cref="T:System.Text.StringBuilder" /> object to write to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="sb" /> is <see langword="null" />.</exception>
    public StringBuilderTextWriter(StringBuilder sb)
        : this(sb, (IFormatProvider) CultureInfo.CurrentCulture) { }

    /// <summary>Initializes a new instance of the <see cref="T:System.IO.StringWriter" /> class that writes to the specified <see cref="T:System.Text.StringBuilder" /> and has the specified format provider.</summary>
    /// <param name="sb">The <see cref="T:System.Text.StringBuilder" /> object to write to.</param>
    /// <param name="formatProvider">An <see cref="T:System.IFormatProvider" /> object that controls formatting.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="sb" /> is <see langword="null" />.</exception>
    public StringBuilderTextWriter(StringBuilder sb, IFormatProvider formatProvider)
        : base(formatProvider) {
        this._sb = sb ?? throw new ArgumentNullException(nameof(sb), "Environment.GetResourceString(\"ArgumentNull_Buffer\")");
        this._isOpen = true;
    }

    /// <summary>Closes the current <see cref="T:System.IO.StringWriter" /> and the underlying stream.</summary>
    public override void Close() =>
        this.Dispose(true);

    /// <summary>Releases the unmanaged resources used by the <see cref="T:System.IO.StringWriter" /> and optionally releases the managed resources.</summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing) {
        this._isOpen = false;
        base.Dispose(disposing);
    }

    /// <summary>Gets the <see cref="T:System.Text.Encoding" /> in which the output is written.</summary>
    /// <returns>The <see langword="Encoding" /> in which the output is written.</returns>

    public override Encoding Encoding {
        get {
            if (m_encoding == null)
                m_encoding = new UnicodeEncoding(false, false);
            return (Encoding) m_encoding;
        }
    }

    /// <summary>Returns the underlying <see cref="T:System.Text.StringBuilder" />.</summary>
    /// <returns>The underlying <see langword="StringBuilder" />.</returns>
    public virtual StringBuilder GetStringBuilder() =>
        this._sb;

    /// <summary>Writes a character to the string.</summary>
    /// <param name="value">The character to write.</param>
    /// <exception cref="T:System.ObjectDisposedException">The writer is closed.</exception>
    public override void Write(char value) {
        if (!this._isOpen)
            throw new ObjectDisposedException((string) null, ("ObjectDisposed_WriterClosed"));

        this._sb.Append(value);
    }

    /// <summary>Writes a subarray of characters to the string.</summary>
    /// <param name="buffer">The character array to write data from.</param>
    /// <param name="index">The position in the buffer at which to start reading data.</param>
    /// <param name="count">The maximum number of characters to write.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
    /// <exception cref="T:System.ArgumentException">(<paramref name="index" /> + <paramref name="count" />)&gt; <paramref name="buffer" />. <see langword="Length" />.</exception>
    /// <exception cref="T:System.ObjectDisposedException">The writer is closed.</exception>
    public override void Write(char[] buffer, int index, int count) {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer), "Environment.GetResourceString(\"ArgumentNull_Buffer\")");
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Environment.GetResourceString(\"ArgumentOutOfRange_NeedNonNegNum\")");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Environment.GetResourceString(\"ArgumentOutOfRange_NeedNonNegNum\")");
        if (buffer.Length - index < count)
            throw new ArgumentException("Environment.GetResourceString(\"Argument_InvalidOffLen\")");
        if (!this._isOpen)
            throw new ObjectDisposedException((string) null, ("ObjectDisposed_WriterClosed"));

        this._sb.Append(buffer, index, count);
    }

    /// <summary>Writes a string to the current string.</summary>
    /// <param name="value">The string to write.</param>
    /// <exception cref="T:System.ObjectDisposedException">The writer is closed.</exception>
    public override void Write(string value) {
        if (!this._isOpen)
            throw new ObjectDisposedException((string) null, ("ObjectDisposed_WriterClosed"));
        if (value == null)
            return;
        this._sb.Append(value);
    }

    /// <summary>Writes a character to the string asynchronously.</summary>
    /// <param name="value">The character to write to the string.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="T:System.ObjectDisposedException">The string writer is disposed.</exception>
    /// <exception cref="T:System.InvalidOperationException">The string writer is currently in use by a previous write operation.</exception>
    public override Task WriteAsync(char value) {
        this.Write(value);
        return Task.CompletedTask;
    }

    /// <summary>Writes a string to the current string asynchronously.</summary>
    /// <param name="value">The string to write. If <paramref name="value" /> is <see langword="null" />, nothing is written to the text stream.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="T:System.ObjectDisposedException">The string writer is disposed.</exception>
    /// <exception cref="T:System.InvalidOperationException">The string writer is currently in use by a previous write operation.</exception>
    public override Task WriteAsync(string value) {
        this.Write(value);
        return Task.CompletedTask;
    }

    /// <summary>Writes a subarray of characters to the string asynchronously.</summary>
    /// <param name="buffer">The character array to write data from.</param>
    /// <param name="index">The position in the buffer at which to start reading data.</param>
    /// <param name="count">The maximum number of characters to write.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException">The <paramref name="index" /> plus <paramref name="count" /> is greater than the buffer length.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
    /// <exception cref="T:System.ObjectDisposedException">The string writer is disposed.</exception>
    /// <exception cref="T:System.InvalidOperationException">The string writer is currently in use by a previous write operation.</exception>
    public override Task WriteAsync(char[] buffer, int index, int count) {
        this.Write(buffer, index, count);
        return Task.CompletedTask;
    }

    /// <summary>Writes a character followed by a line terminator asynchronously to the string.</summary>
    /// <param name="value">The character to write to the string.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="T:System.ObjectDisposedException">The string writer is disposed.</exception>
    /// <exception cref="T:System.InvalidOperationException">The string writer is currently in use by a previous write operation.</exception>
    public override Task WriteLineAsync(char value) {
        this.WriteLine(value);
        return Task.CompletedTask;
    }

    /// <summary>Writes a string followed by a line terminator asynchronously to the current string.</summary>
    /// <param name="value">The string to write. If the value is <see langword="null" />, only a line terminator is written.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="T:System.ObjectDisposedException">The string writer is disposed.</exception>
    /// <exception cref="T:System.InvalidOperationException">The string writer is currently in use by a previous write operation.</exception>
    public override Task WriteLineAsync(string value) {
        this.WriteLine(value);
        return Task.CompletedTask;
    }

    /// <summary>Writes a subarray of characters followed by a line terminator asynchronously to the string.</summary>
    /// <param name="buffer">The character array to write data from.</param>
    /// <param name="index">The position in the buffer at which to start reading data.</param>
    /// <param name="count">The maximum number of characters to write.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException">The <paramref name="index" /> plus <paramref name="count" /> is greater than the buffer length.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
    /// <exception cref="T:System.ObjectDisposedException">The string writer is disposed.</exception>
    /// <exception cref="T:System.InvalidOperationException">The string writer is currently in use by a previous write operation.</exception>
    public override Task WriteLineAsync(char[] buffer, int index, int count) {
        this.WriteLine(buffer, index, count);
        return Task.CompletedTask;
    }

    /// <summary>Asynchronously clears all buffers for the current writer and causes any buffered data to be written to the underlying device.</summary>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    public override Task FlushAsync() =>
        Task.CompletedTask;

    /// <summary>Returns a string containing the characters written to the current <see langword="StringWriter" /> so far.</summary>
    /// <returns>The string containing the characters written to the current <see langword="StringWriter" />.</returns>
    public override string ToString() =>
        this._sb.ToString();
}