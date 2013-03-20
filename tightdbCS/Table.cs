﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

//Tell compiler to give warnings if we publicise interfaces that are not defined in the cls standard
//http://msdn.microsoft.com/en-us/library/bhc3fa7f.aspx
[assembly: CLSCompliant(true)]

//Table class. The class represents a tightdb table.
//implements idisposable - will clean itself up (and any c++ resources it uses) when garbage collected
//If You plan to save resources, You can use it with the using syntax.


//custom exception for Table class. When Table runs into a Table related error, TableException is thrown
//some system exceptions might also be thrown, in case they have not much to do with Table operation
//following the pattern described here http://msdn.microsoft.com/en-us/library/87cdya3t.aspx
public class TableException : Exception
{
    public TableException()
    {
    }

    public TableException(string message)
        : base(message)
    {
    }

    public TableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}


namespace tightdb.Tightdbcsharp
{
    public class TableField1
    {
        static void setinfo(TableField1 t, String ColumnName, TDB FieldType)
        {
            t.colname = ColumnName;
            t.type = FieldType;
        }

        public TableField1(String ColumnName, TableField1[] SubtablefieldsArray)
        {
            setinfo(this, ColumnName, TDB.Table);
            subtable.AddRange(SubtablefieldsArray);
        }

        public TableField1(string ColumnName, TDB ColumnType)
        {
            setinfo(this, ColumnName, ColumnType);
        }


        public String colname;
        public TDB type;
        public List<TableField1> subtable = new List<TableField1>();
    }



    public class Table : IDisposable
    {

        //following the dispose pattern discussed here http://dave-black.blogspot.dk/2011/03/how-do-you-properly-implement.html
        //a good explanation can be found here http://stackoverflow.com/questions/538060/proper-use-of-the-idisposable-interface

        //called by users who don't want to use our class anymore.
        //should free managed as well as unmanaged stuff

        private bool IsDisposed { get; set; }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);//tell finalizer it does not have to call dispose or dispose of things -we have done that already
        }
        //if called from GC  we should not dispose managedas that is unsafe, the bool tells us how we were called
        public void Dispose(bool disposemanagedtoo)
        {
            if (!IsDisposed) 
            {
                if (disposemanagedtoo) {
                    //dispose any managed members table might have
                }

                //dispose any unmanaged stuff we have
                unbind();
            }

        }

        //when tihs is called by the GC, unmanaged stuff might not have been freed, and managed stuff could be in the process of being
        //freed, so only get rid of unmanaged stuff
        ~Table()
        {
            try
            {
                Dispose(false);
            }
            finally
            {
                // Only use this line if Table starts to inherit from some other class that itself implements dispose
                //                base.Dispose();
            }
        }

        
        public Table()
        {
        }

        public Table(TableField1[] schema)
        {
            table_new();//allocate a table class in c++
            Spec spec = get_spec();//get a handle to the table's new empty spec

            foreach (TableField1 kvp in schema)
            {
                TableField1 t = (TableField1)kvp;

                TDB type = t.type;
                String columnname = t.colname;

                if (type != TDB.Table)
                {
                    register_column(columnname, type);
                }
                else
                {
                    TableField1[] tfa = kvp.subtable.ToArray();
                    Table st = new Table(tfa); //create a table with a comitted spec and all from the subtable specification                    
                    spec.add_column_table(st.get_spec(), columnname);//NOT SURE IF A SUBTABLE IS TO BE DEFINED FROM AN ALREADY CREATED TABLE SPEC. IF NOT THIS MUST BE REFACTORED TO FIRST CREATING SPECS , THEN STITCHING THEM UP
                }
            }
            updatefromspec();//build table from the spec
        }

        /*
        //Quite fast - types assumed correct when running,but Asstring needs to create an object. not good
        customers.Asstring(12,3)  = "Hans";
        customers.Asstring(12,"Firstname")  ="Hans";
        //A bit less fast - types have to be lloked up in C# to determine the correct call Asstring could be a property so no object needed
        customers[12,"Firstname"].Asstring = "Hans";
        //untyped, expects object, getter and setter figures what to do
        customers[12,3] = "Hans";
        costomers[12,"firstname"]  ="Hans";
        //if it is a mixed, or a table  such n object is returned
        */

        public object this[int RowIndex, String ColumnName]
        {
            get
            {
                switch (column_type(RowIndex))
                {
                    case TightDbDataType.Int:
                        return getInt(RowIndex, get_column_index(ColumnName));
                    default:
                        return null;//we should probably raise an exception here
                }
            }
            set
            {
            }


        }

        public object this[int RowIndex, int ColumnIndex]
        {
            get
            {
                switch (column_type(RowIndex))
                {
                    case TightDbDataType.Int:
                        return getInt(ColumnIndex, RowIndex);
                    default:
                        return null;
                }
            }
            set
            {
            }
        }

        //not accessible by source not in the TightDBCSharp namespace
        internal UIntPtr TableHandle { get; set; }  //handle (in fact a pointer) to a c++ hosted Table. We must unbind this handle if we have acquired it
        internal bool TableHandleInUse {get; set;} //defaults to false.  TODO:this might need to be encapsulated with a lock to make it thread safe (although several threads *opening or closing* *the same* table object is totally forbidden )

        //This method will ask TDB to create a new table object and then store the TDB table objects handle
        //inside this table Should not be called by users, internal use
        internal void table_new()
        {
            if (TableHandleInUse)
            {
                throw new TableException("table_new called on a table that already has aqcuired a table handle");
            }
            else
            {
                TightDBCalls.table_new(this);
                TableHandleInUse = true;
            }
        }



        //This method will ask TDB to dispose of a table object created by table_new.
        //this method is for internal use only
        //it will automatically be called when the table object is disposed
        //In fact, you should not all it on your own
        internal void unbind()
        {
            if (TableHandleInUse)
            {
                TightDBCalls.table_unbind(this);
                TableHandleInUse = false;
            }
            else
            {
                throw  new TableException("table_unbin called on a table with no table handle active");
            }
        }

        //Users should not really bother with the spec class so it is intrenal for the TightDBCsharp namespace
        internal Spec get_spec()
        {
            return TightDBCalls.table_get_spec(this);
        }

        //this will update the table structure to represent whatever the earlier recieved spec has been set up to
        internal void updatefromspec()
        {
            tightdb.Tightdbcsharp.TightDBCalls.table_update_from_spec(this);
        }


        public TightDbDataType column_type(long ColumnIndex)
        {
            return TightDBCalls.table_get_column_type(this, ColumnIndex);
        }

        //            size_t      table_register_column(Table *t,  TightdbDataType type, const char *name);
        public long register_column(String name, TDB type)
        {
            return TightDBCalls.table_register_column(this, type, name);
        }

        public long column_count()
        {
            return TightDBCalls.table_get_column_count(this);
        }

        public long get_column_index(string name)
        {
            return TightDBCalls.table_get_column_index(this, name);
        }

        public string get_column_name(long col_idx)
        {
            return TightDBCalls.table_get_column_name(this, col_idx);
        }

        public long add()
        {
            return TightDBCalls.table_add(this);
        }

        public long insert(long ColIx)
        {
            return TightDBCalls.table_insert(this, ColIx);
        }

        public long getInt(long recordix, long colix)
        {
            return TightDBCalls.table_get_int(this, colix, recordix);
        }

    }

}

//various ideas for doing what is done with c++ macros reg. creation of typed tables
//An extern method that creates the table on any class tha the extern might be called on. The extren would then have to 
//use reflection to figure what fields should be stored
//would mean that You annotate all fileds that should go into the table database
// + easy to use on existing classes,  
// + easy to build a new class using well known syntax and tools
// - could easily fool user to use unsupported types, 
// - user will not have a strong typed table classs, only his own class which is of whatever type
// - User would have to annotate fields to be put in the database. Default could be no field means all goes in
// + if this was just one of many ways to create a table, it could be okay - in some cases it might be convenient

// see implementation at //EXAMPLE1
